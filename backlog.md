# Backlog пилота WeatherWithDocker

> Цель пилота: откатать гипотезу из `D:\repos\dodo-is` — **Подход 2 (Coverlet Global Tool)**
> из `workarounds.md`. Нужно собрать code coverage с .NET-приложения, которое работает
> в Docker-контейнере и тестируется по HTTP (как интеграционные тесты монолита dodo-is),
> через graceful shutdown контейнера.
>
> Источники гипотезы:
> - `D:\repos\dodo-is\plan.md` (общий план сбора coverage в пайплайне)
> - `D:\repos\dodo-is\workarounds.md` (шаг/подход 2 — Coverlet Global Tool как обёртка запуска)

---

## Что это за проект

Стандартный ASP.NET Core Weather API (`net10.0`, SDK `Microsoft.NET.Sdk.Web`).
Маршрут для проверки покрытия: `GET /WeatherForecast` (`WeatherForecastController`).
Запускается из Visual Studio (активный профиль `Container (Dockerfile)`, конфигурация Debug).

---

## Что сделано сегодня

### 1. Переписан `WeatherWithDocker/Dockerfile`

Исходный черновик был нерабочим: `coverlet.console` ставился в stage `publish`, а
`ENTRYPOINT` выполнялся в stage `final` (на `aspnet`-runtime без SDK и под `USER $APP_UID`) —
инструмент туда не попадал, прав не было.

Текущее состояние Dockerfile (закоммичено в рабочую копию):
- `final` базируется на **`mcr.microsoft.com/dotnet/sdk:10.0`** (нужен `dotnet tool install`,
  `dotnet` driver и совпадающий runtime для самого coverlet), работает под **root**.
- `coverlet.console` ставится именно в `final`, `PATH` включает `/root/.dotnet/tools`.
- Создаётся каталог `/coverage` (в рантайме сюда монтируется volume).
- Явно задан порт: `ENV ASPNETCORE_HTTP_PORTS=8080` (SDK-образ его не выставляет).
- `STOPSIGNAL SIGINT` (см. нюанс про сигналы ниже).
- `ENTRYPOINT` в **exec-форме** (coverlet = PID 1):
  ```
  ENTRYPOINT ["coverlet", "/app", "--target", "dotnet", "--targetargs", "/app/WeatherWithDocker.dll", "--output", "/coverage/coverage.json", "--format", "json"]
  ```

### 2. Проведена серия диагностических прогонов через `docker run`/`docker exec`

Образ собирается и запускается, приложение отвечает `200` на `/WeatherForecast`.

---

## Ключевые находки и нюансы (ВАЖНО)

### A. Visual Studio в Debug = Fast Mode → наш ENTRYPOINT игнорируется
В Debug VS Docker-tools работают в **Fast Mode**: собирают только stage `base`, монтируют
проект и **подменяют `ENTRYPOINT` своим**. Значит coverlet-обёртка в Fast Mode НЕ запускается.
→ Для сбора coverage из VS нужен **Regular Mode** (`<ContainerDevelopmentMode>Regular</ContainerDevelopmentMode>`)
или запуск образа напрямую через docker.

### B. Coverlet не флашит отчёт по SIGTERM; нужен SIGINT именно дочернему процессу
- `docker stop` по умолчанию шлёт **SIGTERM** в PID 1 (coverlet). Coverlet на SIGTERM
  завершается без сброса отчёта (exit code 130/143, файл не пишется).
- Добавление `STOPSIGNAL SIGINT` **не помогло**: coverlet (PID 1) получает SIGINT, но
  **не пробрасывает** его дочернему `dotnet` → graceful shutdown ASP.NET не наступает.
- **Рабочий способ** (проверено): послать SIGINT напрямую внутреннему процессу:
  `kill -INT $(pidof dotnet)` внутри контейнера → ASP.NET корректно гасится
  («Application is shutting down...») → coverlet печатает «Calculating coverage result...»
  и пишет файл.
- Вывод: **проблема форвардинга сигналов от PID 1 (coverlet) к дочернему dotnet** не решена.
  Кандидаты на решение (на след. раз):
  1. **Shutdown-эндпоинт** в приложении (`IHostApplicationLifetime.StopApplication()`),
     который «дёргается маршрутом» — самый чистый вариант под желаемый сценарий
     «запустить → дёрнуть маршрут → получить файл». При нормальном выходе target-процесса
     coverlet (родитель) сам собирает покрытие, форвардинг сигналов вообще не нужен.
  2. init/wrapper, который форвардит сигнал ТОЛЬКО дочернему dotnet (не самому coverlet).
     Внимание: `tini -g` шлёт сигнал всей группе, включая coverlet → НЕ подходит.

### C. ГЛАВНЫЙ блокер: coverlet НЕ инструментирует `WeatherWithDocker.dll` из образа
Даже когда отчёт успешно генерируется, он **пустой**: `coverage.json` = `{}` (2 байта),
cobertura = `<packages />`, Total 0%.

Диагностика:
- coverlet версии **10.0.1**, .NET 10 — на **тривиальном console-app** инструментирует и даёт
  **100%** (значит coverlet + net10 в этом образе исправны в принципе).
- На сборке из образа (`/app`) coverlet применяет фильтры (видно `Excluded module` для
  Microsoft.*), но про `WeatherWithDocker.dll` молчит — **ни «Instrumented module», ни
  предупреждения**. Т.е. `Instrumenter.CanInstrument()` вернул false (скорее всего
  `HasPdb` не сматчил pdb), и coverlet тихо пропустил сборку.
- PDB физически присутствует рядом (`/app/WeatherWithDocker.pdb`, ~31 КБ).
- **Debug-сборка** того же проекта в контейнере инструментируется (`Instrumented module:
  .../WeatherWithDocker.dll`).
- Любой `dotnet publish` из исходников, где присутствовали **host-собранные `obj/`/`bin/`**
  (папка скопирована `docker cp` с хоста), — инструментируется (Release и Debug, p1..p4).
- **Чистая** сборка/publish внутри Linux-контейнера (без host-артефактов) — **НЕ
  инструментируется**. Перебор `DebugType` (`portable`/`full`/`embedded`) и
  `Deterministic=false` на чистом publish **не помог**.
- Бинарно `/app/WeatherWithDocker.{dll,pdb}` (из образа) и свежий чистый publish
  **отличаются** (разные dll и pdb), хотя оба «Release publish».

Промежуточный вывод: проблема в **pdb, который генерит чистая Linux-сборка этого Web-проекта**
(coverlet `HasPdb`/`CanInstrument` его не принимает). Почему host-obj-сборки проходят —
пока не объяснено (возможно, MSBuild incremental переиспользует host-pdb).

**НЕ доделано:** не снят полный (без grep) verbose-лог coverlet на чистой сборке и не
проинспектирована debug-directory dll (CodeView path + GUID/age, portable vs windows pdb).
Это первый шаг на след. сессию.

---

## Что нужно для сценария «запустить из VS → дёрнуть маршрут → увидеть файл покрытия»

1. **Починить инструментацию** (находка C) — без этого файл будет пустой. Корень — pdb/HasPdb.
2. **Триггер сброса покрытия маршрутом** — добавить shutdown-эндпоинт
   (`IHostApplicationLifetime.StopApplication()`); попутно решает и проблему сигналов (B).
3. **Volume `/coverage` → папка на хосте**, иначе файл не виден снаружи. Для VS — прокинуть
   через `DockerfileRunArguments` в csproj или `launchSettings.json`; для docker —
   `-v <host>:/coverage`.
4. **Regular Mode для VS** (находка A), иначе coverlet-entrypoint не выполнится.

---

## Открытые вопросы / гипотезы на проверку

- Почему именно чистая Linux-сборка Web-SDK проекта даёт неинструментируемый pdb, а
  console-app и host-obj-сборки — инструментируются? (Web SDK vs Sdk? static web assets?
  отличие portable-pdb?)
- Снять полный verbose coverlet + дамп debug-directory (System.Reflection.Metadata.PEReader)
  на «плохой» и «хорошей» dll, сравнить CodeView/GUID/age и тип pdb.
- Проверить, не помогает ли `<DebugType>portable</DebugType>` + `<DebugSymbols>true</DebugSymbols>`
  заданные В CSPROJ (а не через /p:) на чистой сборке; и `CopyOutputSymbolsToPublishDirectory`.
- Возможный обход: инструментировать сборку из stage `build` (Debug-вывод инструментируется),
  а не из `publish`.

---

## Точки входа для следующей сессии

- Файл: `WeatherWithDocker/Dockerfile` (текущая рабочая обёртка coverlet).
- Команда сборки образа:
  `docker build -t weatherwithdocker:coverage -f WeatherWithDocker\Dockerfile .`
- Воспроизведение пустого отчёта:
  `docker run -d -p 8080:8080 -v <host>:/coverage weatherwithdocker:coverage`,
  затем `curl /WeatherForecast`, затем `docker exec <c> sh -c 'kill -INT $(pidof dotnet)'`.
- Рабочая проверка инструментации (изолированно от web/SIGINT):
  заинструментировать каталог сборки, а target подсунуть тривиальный console-app, который
  сразу завершается — в логе видно «Instrumented module» либо его отсутствие.

## Текущие временные артефакты (можно удалить)
- Папка `coverage-out/` в корне (результаты прогонов).
- Тестовые контейнеры `weather-cov`, `dbg` (если остались).
