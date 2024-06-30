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
3. Перезапустить `ArchiSteamFarm` , и используйте команду `ATE` для проверки работоспособности плагина

### Команды для обновления

> 支持 ASF 自带的插件更新机制
> 使用 `UPDATEPLUGINS stable ASFTradeExtension` 更新插件

### Журнал изменений

| Версия ASFTradeExtension                                                    | Совместимая версия ASF | Описание                                                                         |
| --------------------------------------------------------------------------- | :--------------------: | -------------------------------------------------------------------------------- |
| [1.1.1.1](https://github.com/chr233/ASFTradeExtension/releases/tag/1.1.1.1) |        6.0.3.4         | ASF -> 6.0.3.4, 支持闪卡                                                         |
| [1.1.0.0](https://github.com/chr233/ASFTradeExtension/releases/tag/1.1.0.0) |        6.0.0.3         | ASF -> 6.0.0.3                                                                   |
| [1.0.9.0](https://github.com/chr233/ASFTradeExtension/releases/tag/1.0.9.0) |        5.5.0.11        | ASF -> 5.5.0.11, 新的缓存机制                                                    |
| [1.0.8.0](https://github.com/chr233/ASFTradeExtension/releases/tag/1.0.8.0) |        5.4.10.3        | ASF -> 5.4.10.3                                                                  |
| [1.0.7.0](https://github.com/chr233/ASFTradeExtension/releases/tag/1.0.7.0) |        5.4.9.3         | ASF -> 5.4.9.3                                                                   |
| [1.0.6.1](https://github.com/chr233/ASFTradeExtension/releases/tag/1.0.6.1) |        5.4.8.3         | Модифицирован некоторый код торговли картами                                     |
| [1.0.6.0](https://github.com/chr233/ASFTradeExtension/releases/tag/1.0.6.0) |        5.4.8.3         | Модифицирован некоторый код, функция торговли картами должна быть протестирована |
| [1.0.2.0](https://github.com/chr233/ASFTradeExtension/releases/tag/1.0.2.0) |        5.4.4.5         | Исправления ошибок                                                               |
| [1.0.0.0](https://github.com/chr233/ASFTradeExtension/releases/tag/1.0.0.0) |        5.4.2.13        | Первая версия                                                                    |

<details>
  <summary>История версий</summary>

| Версия ASFTradeExtension | Совместимая версия ASF | 5.3.1.2 | 5.3.2.4 | 5.4.0.3 | 5.4.1.11 |
| ------------------------ | :--------------------: | :-----: | :-----: | :-----: | :------: |
|                          |           -            |   ❌    |   ❌    |   ✔️    |    ✔️    |

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
    "MaxItemPerTrade": 255,
    "CacheTTL": 600
  }
}
```

| Конфигурация      | Тип    | По умолчанию | Описание                                                                                                                                                                                                 |
| ----------------- | ------ | ------------ | -------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| `EULA`            | bool   | `true`       | Если согласны с [лицензионным соглашением](#лицензионное-соглашение)\*                                                                                                                                   |
| `Statistic`       | bool   | `true`       | Разрешить отправку данных для статистики. Она используется для подсчета количества пользователей, при этом никакой другой информации отправляться не будет                                               |
| `MaxItemPerTrade` | ushort | `255`        | Максимальное количество товаров в одной сделке, по умолчанию значение ASF равно 255, если количество товаров в предложении превысит это число, оно будет автоматически разбито на несколько предложений. |
| `CacheTTL`        | ushort | `600`        | 库存缓存过期时间, 单位秒, 缓存未过期也可使用命令 `RELOADCACHE` 强制刷新缓存                                                                                                                              |

## Использование Команд

### Команды Обновления

| Команда             | Сокращение | Доступ          | Описание                          |
| ------------------- | ---------- | --------------- | --------------------------------- |
| `ASFTradeExtension` | `ATE`      | `FamilySharing` | Получить версию ASFTradeExtension |

### Торговля картами

| Command                                            | Shorthand | Access     | Description                                                                                                           |
| -------------------------------------------------- | --------- | ---------- | --------------------------------------------------------------------------------------------------------------------- |
| `FULLSETLIST [Bots] [Config]`                      | `FSL`     | `Operator` | Display normal card inventory information, available parameters \[-page Page number\] \[-line Display line number\]   |
| `FULLSETLISTFOIL [Bots] [Config]`                  | `FSLF`    | `Operator` | Display foil card inventory information, available parameters \[-page Page number\] \[-line Display line number\]     |
| `FULLSETLISTSALE [Bots]`                           | `FSLS`    | `Operator` | Display sale event card inventory information                                                                         |
| `FULLSET [Bots] <appIds>`                          | `FS`      | `Operator` | Display normal card information on the number of cards by the specified `AppId`                                       |
| `FULLSETFOIL [Bots] <appIds>`                      | `FSF`     | `Operator` | Display foil card information on the number of cards by the specified `AppId`                                         |
| `SENDCARDSET [Bots] AppId SetCount TradeLink`      | `SCS`     | `Master`   | Sends a set of normal cards with the specified `SetCount` and `AppId` to the trade link                               |
| `SENDCARDSETFOIL [Bots] AppId SetCount TradeLink`  | `SCSF`    | `Master`   | Sends a set of foil cards with the specified `SetCount` and `AppId` to the trade link                                 |
| `2SENDCARDSET [Bots] AppId SetCount TradeLink`     | `2SCS`    | `Master`   | Similar to `SENDCARDSET`, automatically confirms a trade after it has been sent (requires 2FA to be added to ASF)     |
| `2SENDCARDSETFOIL [Bots] AppId SetCount TradeLink` | `2SCSF`   | `Master`   | Similar to `SENDCARDSETFOIL`, automatically confirms a trade after it has been sent (requires 2FA to be added to ASF) |

---

[![Repobeats analytics image](https://repobeats.axiom.co/api/embed/c7bad85b243c7305a5de1fa591469f64125c4048.svg "Repobeats analytics image")](https://github.com/chr233/ASFTradeExtension/pulse)

---

[![Stargazers over time](https://starchart.cc/chr233/ASFTradeExtension.svg)](https://github.com/chr233/ASFTradeExtension/stargazers)

---
