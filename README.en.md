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

[中文说明](README.md) | [Русская Версия](README.ru.md)

## EULA

> Please don't use this plugin to conduct repulsive behaviors, including but not limited to: post fake reviews, posting advertisements, etc
>
> See [Plugin Configuration](#plugin-configuration)

## Installation

Not compatible with generic version of ASF, please use generic version of ASF (not generic-nf).
Not compatible with generic version of ASF, please use generic version of ASF (not generic-nf).

Not compatible with the common version of ASF currently, use generic version of ASF instead (not generic-nf version)
Not compatible with the common version of ASF currently, use generic version of ASF instead (not generic-nf version)

### First-Time Install / Manually Update

1. Download the plugin via [GitHub Releases](https://github.com/chr233/ASFTradeExtension/releases) page
2. Unzip the `ASFTradeExtension.dll` and copy it into the `plugins` folder in the `ArchiSteamFarm`'s directory
3. Restart the `ArchiSteamFarm` and use `ATE` command to check if the plugin is working

### Use Command to Update

> You can update the plugin by using the command that comes with the plugin.
> ASF version upgrade may be incompatible, if you find that the plugin can not be loaded, please try to update ASF

- `ATEVERSION` / `ATEV` check the latest version of ASFTradeExtension
- `ATEUPDATE` / `ATEU` auto update ASFTradeExtension (Maybe need to update ASF manually)

### ChangeLog

| ASFTradeExtension Version                                                   | Compatible ASF version | Description                                              |
| --------------------------------------------------------------------------- | :--------------------: | -------------------------------------------------------- |
| [1.0.8.0](https://github.com/chr233/ASFTradeExtension/releases/tag/1.0.8.0) |   5.4.10.3             | ASF -> 5.4.10.3                                          |
| [1.0.7.0](https://github.com/chr233/ASFTradeExtension/releases/tag/1.0.7.0) |    5.4.9.3             | ASF -> 5.4.9.3                                           |
| [1.0.6.1](https://github.com/chr233/ASFTradeExtension/releases/tag/1.0.6.1) |    5.4.8.3             | Modified some code, card trading function to be tested   |
| [1.0.2.0](https://github.com/chr233/ASFTradeExtension/releases/tag/1.0.2.0) |    5.4.4.5             | Bug fixes                                                |
| [1.0.0.0](https://github.com/chr233/ASFTradeExtension/releases/tag/1.0.0.0) |   5.4.2.13             | First version                                            |

<details>
  <summary>History Version</summary>

| ASFTradeExtension Version | Depended ASF  | 5.3.1.2 | 5.3.2.4 | 5.4.0.3 | 5.4.1.11 |
| ------------------------- | :-----------: | :-----: | :-----: | :-----: | :------: |
| -                         |       -       |   ❌    |   ❌    |   ✔️    |    ✔️    |

</details>

## Plugin Configuration

> The configuration of this plugin is not required, and most functions is available in default settings

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

| Configuration     | Type   | Default | Description                                                                        |
| ----------------- | ------ | ------- | ---------------------------------------------------------------------------------- |
| `EULA`            | bool   | `true`  | If agree the [EULA](#EULA)\*                                                       |
| `Statistic`       | bool   | `true`  | Allow send statistics data, it's used to count number of users, this will not send any other information |
| `DisabledCmds`    | list   | `null`  | Optional, Cmd in the list will be disabled\*\* , **Case Insensitive**, only effects on `ASFTradeExtension` cmds    |
| `MaxItemPerTrade` | ushort | `255`   | Maximum number of items in a single transaction, the default value of ASF is 255, if the number of items in the offer exceeds this number, it will be automatically split into more than one         |

> \* When Agree [EULA](#EULA), ASFEnhance will let all commands available
>
> \* When Disagree [EULA](#EULA), ASFTradeExtension will not be able to use most commands.
>
> \*\* `DisabledCmds` description: every item in this configuration is **Case Insensitive**, and this only effects on `ASFTradeExtension` cmds
> For example, configure as `["foo","BAR"]` , it means `FOO` and `BAR` will be disabled
> If don't want to disable any cmds, please configure as `null` or `[]`
> If Some cmd is disabled, it's still available to call the command in the form of `ATE.xxx`, for example `ATE.FULLSETLIST`

## Commands Usage

### Update Commands

| Command             | Shorthand | Access          | Description                                                  |
| ------------------- | --------- | --------------- | ----------------------------------------------------- |
| `ASFTradeExtension` | `ATE`     | `FamilySharing` | Get the version of the ASFTradeExtension                           |
| `ATEVERSION`        | `ATEV`    | `Operator`      | Check ASFTradeExtension's latest version                   |
| `ATEUPDATE`         | `ATEU`    | `Owner`         | Update ASFTradeExtension to the latest version (need restart ASF manually)   |

### Card Trading

| Command                                        | Shorthand | Access        | Description                                                    |
| ---------------------------------------------- | --------- | ---------- | ------------------------------------------------------  |
| `FULLSETLIST [Bots] [Config]`                  | `FSL`     | `Operator` | Display CS2 inventory information, available parameters \[-page Page number\] \[-line Display line number\] |
| `FULLSET [Bots] <appIds>`                      | `FS`      | `Operator` | Display information on the number of cards by the specified `AppId`                                |
| `SENDCARDSET [Bots] AppId SetCount TradeLink`  | `SCS`     | `Master`   | Sends a set of cards with the specified `SetCount` and `AppId` to the trade link             |
| `2SENDCARDSET [Bots] AppId SetCount TradeLink` | `2SCS`    | `Master`   | Similar to `SENDCARDSET`, automatically confirms a trade after it has been sent (requires 2FA to be added to ASF)      |

### CS2 inventory trading

| Command                                  | Shorthand   | Access        | Description                                                                  |
| ---------------------------------------- | ------ | ---------- | --------------------------------------------------------------------- |
| `CSITEMLIST [Bots] [Config]`             | `CIL`  | `Operator` | Display CS2 inventory information, available parameters \[-page Page number\] \[-line Display line number\]               |
| `CSSENDITEM [Bots]`                      | `CSI`  | `Master`   | Send Bots' CS2 inventory to the remaining online Bot                                    |
| `2CSSENDITEM [Bots]`                     | `2CSI` | `Master`   | Similar to `CSSENDITEM`, automatically confirms the transaction after sending it (requires 2FA configuration)                     |
| `CSSENDITEM [Bots] ClassId CountPerBot`  | `CSI`  | `Master`   | Sends a specific CS2 item to bots on a list, specify the `ClassId` of the item and the number of items received by each bot |
| `2CSSENDITEM [Bots] ClassId CountPerBot` | `2CSI` | `Master`   | Similar to `SENDCARDSET`, automatically confirms a trade after it has been sent (requires 2FA to be added to ASF)                    |

---

[![Repobeats analytics image](https://repobeats.axiom.co/api/embed/c7bad85b243c7305a5de1fa591469f64125c4048.svg "Repobeats analytics image")](https://github.com/chr233/ASFTradeExtension/pulse)

---

[![Stargazers over time](https://starchart.cc/chr233/ASFTradeExtension.svg)](https://github.com/chr233/ASFTradeExtension/stargazers)

---
