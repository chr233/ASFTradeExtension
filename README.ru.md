# ASFTradeExtension (原 CardTradeExtension)

[![Codacy Badge](https://app.codacy.com/project/badge/Grade/45b50288f8b14ebda915ed89e0382648)](https://www.codacy.com/gh/chr233/ASFTradeExtension/dashboard)
![GitHub Workflow Status](https://img.shields.io/github/actions/workflow/status/chr233/ASFTradeExtension/autobuild.yml?logo=github)
[![License](https://img.shields.io/github/license/chr233/ASFTradeExtension?logo=apache)](https://github.com/chr233/ASFTradeExtension/blob/master/license)

[![GitHub Release](https://img.shields.io/github/v/release/chr233/ASFTradeExtension?logo=github)](https://github.com/chr233/ASFTradeExtension/releases)
[![GitHub Release](https://img.shields.io/github/v/release/chr233/ASFTradeExtension?include_prereleases&label=pre-release&logo=github)](https://github.com/chr233/ASFTradeExtension/releases)
![GitHub last commit](https://img.shields.io/github/last-commit/chr233/ASFTradeExtension?logo=github)

![GitHub Repo stars](https://img.shields.io/github/stars/chr233/ASFTradeExtension?logo=github)
[![GitHub Download](https://img.shields.io/github/downloads/chr233/ASFTradeExtension/total?logo=github)](https://img.shields.io/github/v/release/chr233/ASFTradeExtension)

[![Bilibili](https://img.shields.io/badge/bilibili-Chr__-00A2D8.svg?logo=bilibili)](https://space.bilibili.com/5805394)
[![Steam](https://img.shields.io/badge/steam-Chr__-1B2838.svg?logo=steam)](https://steamcommunity.com/id/Chr_)

[![Steam](https://img.shields.io/badge/steam-donate-1B2838.svg?logo=steam)](https://steamcommunity.com/tradeoffer/new/?partner=221260487&token=xgqMgL-i)
[![爱发电](https://img.shields.io/badge/爱发电-chr__-ea4aaa.svg?logo=github-sponsors)](https://afdian.net/@chr233)

[中文说明](README.md) | [English Version](README.en.md)

## ЛИЦЕНЗИОННОЕ СОГЛАШЕНИЕ

> Пожалуйста, не используйте этот плагин для отвратительного поведения, включая, но не ограничиваясь: публикацией фейковых отзывов, размещением рекламы и т.д.
>
> Смотри [Конфигурацию Плагина](#Конфигурация-плагина)

## Установка

\* Необходимая релизная версия ASF (**НЕ** предварительно выпущенная)

### Первая установка / Обновление в ручном режиме

1. Загрузите плагин через страницу [GitHub Releases](https://github.com/chr233/ASFTradeExtension/releases) 
2. Распакуйте файл `ASFTradeExtension.dll` и скопируйте его в папку `plugins` в директории `ArchiSteamFarm`
3. Перезапустить `ArchiSteamFarm` , и используйте команду  `ATE`  для проверки работоспособности плагина

### Команды для обновления

> Для обновления плагина можно использовать собственную команду плагина.
> Обновление версии ASF может быть несовместимым, если вы обнаружили, что плагин не может быть загружен, попробуйте обновить ASF.


- `ATEVERSION` / `ATE` проверить последнюю версию ASFTradeExtension
- `ATEUPDATE` / `ATEU` автоматическое обновление ASFTradeExtension (возможно, потребуется обновить ASF вручную)

### Журнал изменений

| Версия ASFTradeExtension                                                    | Совместимая версия ASF  | Описание                                                                          |
| --------------------------------------------------------------------------- | :---------------------: | --------------------------------------------------------------------------------- |
| [1.0.8.0](https://github.com/chr233/ASFTradeExtension/releases/tag/1.0.8.0) |       5.4.10.3          | ASF -> 5.4.10.3                                                                   |
| [1.0.7.0](https://github.com/chr233/ASFTradeExtension/releases/tag/1.0.7.0) |       5.4.9.3           | ASF -> 5.4.9.3                                                                    |
| [1.0.6.1](https://github.com/chr233/ASFTradeExtension/releases/tag/1.0.6.1) |       5.4.8.3           | Модифицирован некоторый код торговли картами                                      |
| [1.0.6.0](https://github.com/chr233/ASFTradeExtension/releases/tag/1.0.6.0) |       5.4.8.3           | Модифицирован некоторый код, функция торговли картами должна быть протестирована  |
| [1.0.2.0](https://github.com/chr233/ASFTradeExtension/releases/tag/1.0.2.0) |       5.4.4.5           | Исправления ошибок                                                                |
| [1.0.0.0](https://github.com/chr233/ASFTradeExtension/releases/tag/1.0.0.0) |       5.4.2.13          | Первая версия                                                                     |

<details>
  <summary>История версий</summary>

| Версия ASFTradeExtension | Совместимая версия ASF | 5.3.1.2 | 5.3.2.4 | 5.4.0.3 | 5.4.1.11 |
| ---------------------- | :----------------------: | :-----: | :-----: | :-----: | :------: |
|                        |             -            |   ❌    |   ❌    |   ✔️    |    ✔️    |

</details>

## Конфигурация плагина

> Настройка этого плагина не требуется, большинство функций доступно в настройках по умолчанию

ASF.json

```json
{
  //ASF 配置
  "CurrentCulture": "...",
  "IPCPassword": "...",
  "...": "...",
  //ASFTradeExtension 配置
  "ASFTradeExtension": {
    "EULA": true,
    "Statistic": true,
    "DisabledCmds": ["foo", "bar"],
    "MaxItemPerTrade": 255
  }
}
```

| Конфигурация      | Тип    | По умолчанию | Описание                                                                                                                                                                                                 |
| ----------------- | ------ | ------------ | -------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| `EULA`            | bool   | `true`       | Если согласны с [лицензионным соглашением](#лицензионное-соглашение)\*                                                                                                                                   |
| `Statistic`       | bool   | `true`       | Разрешить отправку данных для статистики. Она используется для подсчета количества пользователей, при этом никакой другой информации отправляться не будет                                               |
| `DisabledCmds`    | list   | `null`       | Команды в списке будут отключены , **`DisabledCmds` нечувствительна к командам ASF**, данная конфигурация влияет только на команды `ASFTradeExtension`                                                   |
| `MaxItemPerTrade` | ushort | `255`        | Максимальное количество товаров в одной сделке, по умолчанию значение ASF равно 255, если количество товаров в предложении превысит это число, оно будет автоматически разбито на несколько предложений. |

> \* Если Вы согласны с [лицензионным соглашением](#лицензионное-соглашение), то все команды ASFTradeExtension  будут открыты
>
> \* Если Вы не согласны с [лицензионным соглашением](#лицензионное-соглашение), то большинство команд ASFTradeExtension будет недоступно
>
> \*\* Описание `DisabledCmds`: каждый элемент в этой конфигурации является **нечувствительным к командам ASF**, и это влияет только на команды `ASFTradeExtension`.
> Например, если настроить `["foo", "BAR"]`, то это означает, что `FOO` и `BAR` будут отключены,
> если вы не хотите отключать какие-либо команды, настройте их как `null` или `[]`.
> Если некоторые команды отключены, то все равно можно вызвать команду в виде `ATE.xxx`, например `ATE.FULLSETLIST`

## Использование Команд

### Команды Обновления

| Команда             | Сокращение | Доступ          | Описание                                                                        |
| ------------------- | ---------- | --------------- | ------------------------------------------------------------------------------- |
| `ASFTradeExtension` | `ATE`      | `FamilySharing` | Получить версию ASFTradeExtension                                               |
| `ATEVERSION`        | `ATEV`     | `Operator`      | Проверить последнюю версию ASFTradeExtension                                    |
| `ATEUPDATE`         | `ATEU`     | `Owner`         | Обновить ASFTradeExtensionдо последней версии (необходим ручной перезапуск ASF) |

### Торговля картами

| Команда                                        | Сокращение | Доступ     | Описание                                                                                                                |
| ---------------------------------------------- | ---------- | ---------- | ----------------------------------------------------------------------------------------------------------------------- |
| `FULLSETLIST [Bots] [Config]`                  | `FSL`      | `Operator` | Отображение информации о наборе карт, доступны параметры: \[-page Номер страницы\] \[-line Отображение номера строки\]  |
| `FULLSET [Bots] <appIds>`                      | `FS`       | `Operator` | Отображение информации о количестве карточек по указанному `AppId`                                                      |
| `SENDCARDSET [Bots] AppId SetCount TradeLink`  | `SCS`      | `Master`   | Отправляет набор карточек с указанным `SetCount` и `AppId` по трейд ссылке                                             |
| `2SENDCARDSET [Bots] AppId SetCount TradeLink` | `2SCS`     | `Master`   | Аналогично `SENDCARDSET`, автоматически подтверждает трейд после его отправки (требуется настройка 2FA)                 |

### Торговля инвентарем CS2

| Команда                                  | Сокращение | Доступ     | Описание                                                                                                                |
| ---------------------------------------- | ---------- | ---------- | ----------------------------------------------------------------------------------------------------------------------- |
| `CSITEMLIST [Bots] [Config]`             | `CIL`      | `Operator` | Отображение информации о наборе карт, доступных параметрах \[-page Номер страницы\] \[-line Отображение номера строки\] |
| `CSSENDITEM [Bots]`                      | `CSI`      | `Master`   | Отправляет весь инвентарь CS2 ботам по списку                                                                             |
| `2CSSENDITEM [Bots]`                     | `2CSI`     | `Master`   | Аналогично `CSSENDITEM`, автоматически подтверждает трейд после его отправки (требует добавления 2FA в ASF)              |
| `CSSENDITEM [Bots] ClassId CountPerBot`  | `CSI`      | `Master`   | Отправляет определенный предмет CS2 ботам по списку, укажите `ClassId` предмета и количество предметов, полученных каждым ботом |
| `2CSSENDITEM [Bots] ClassId CountPerBot` | `2CSI`     | `Master`   | Аналогично `SENDCARDSET`, автоматически подтверждает трейд после его отправки (требует добавления 2FA в ASF)             |

---

[![Repobeats analytics image](https://repobeats.axiom.co/api/embed/c7bad85b243c7305a5de1fa591469f64125c4048.svg "Repobeats analytics image")](https://github.com/chr233/ASFTradeExtension/pulse)

---

[![Stargazers over time](https://starchart.cc/chr233/ASFTradeExtension.svg)](https://github.com/chr233/ASFTradeExtension/stargazers)

---
