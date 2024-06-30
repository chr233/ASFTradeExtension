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

ASFTradeExtension 介绍 & 使用指南: [https://keylol.com/t876377-1-1](https://keylol.com/t876377-1-1)

[Русская Версия](README.ru.md) | [English Version](README.en.md)

## EULA

> 请不要使用本插件来进行不受欢迎的行为, 包括但不限于: 刷好评, 发布广告 等.
>
> 详见 [插件配置说明](#插件配置说明)

## 安装方式

不兼容普通版本的 ASF, 请使用 generic 版本的 ASF
不兼容普通版本的 ASF, 请使用 generic 版本的 ASF

Not compatible with the common version of ASF, use generic version of ASF instead
Not compatible with the common version of ASF, use generic version of ASF instead

### 初次安装 / 手动更新

1. 从 [GitHub Releases](https://github.com/chr233/ASFTradeExtension/releases) 下载插件的最新版本
2. 解压后将 `ASFTradeExtension.dll` 丢进 `ArchiSteamFarm` 目录下的 `plugins` 文件夹
3. 重新启动 `ArchiSteamFarm` , 使用命令 `ATE` 来检查插件是否正常工作

### 使用命令升级插件

> 支持 ASF 自带的插件更新机制
> 使用 `UPDATEPLUGINS stable ASFTradeExtension` 更新插件

### 更新日志

| ASFTradeExtension 版本                                                      | 适配 ASF 版本 | 更新说明                             |
| --------------------------------------------------------------------------- | :-----------: | ------------------------------------ |
| [1.1.1.1](https://github.com/chr233/ASFTradeExtension/releases/tag/1.1.1.1) |    6.0.3.4    | ASF -> 6.0.3.4, 支持闪卡             |
| [1.1.0.0](https://github.com/chr233/ASFTradeExtension/releases/tag/1.1.0.0) |    6.0.0.3    | ASF -> 6.0.0.3                       |
| [1.0.9.0](https://github.com/chr233/ASFTradeExtension/releases/tag/1.0.9.0) |   5.5.0.11    | ASF -> 5.5.0.11, 新的缓存机制        |
| [1.0.8.0](https://github.com/chr233/ASFTradeExtension/releases/tag/1.0.8.0) |   5.4.10.3    | ASF -> 5.4.10.3                      |
| [1.0.7.0](https://github.com/chr233/ASFTradeExtension/releases/tag/1.0.7.0) |    5.4.9.3    | ASF -> 5.4.9.3                       |
| [1.0.6.1](https://github.com/chr233/ASFTradeExtension/releases/tag/1.0.6.1) |    5.4.8.3    | 修改了一些代码, 卡牌交易功能有待测试 |
| [1.0.2.0](https://github.com/chr233/ASFTradeExtension/releases/tag/1.0.2.0) |    5.4.4.5    | Bug 修复                             |
| [1.0.0.0](https://github.com/chr233/ASFTradeExtension/releases/tag/1.0.0.0) |   5.4.2.13    | 第一个版本                           |

<details>
  <summary>历史版本</summary>

| ASFTradeExtension 版本 | 依赖 ASF 版本 | 5.3.1.2 | 5.3.2.4 | 5.4.0.3 | 5.4.1.11 |
| ---------------------- | :-----------: | :-----: | :-----: | :-----: | :------: |
| -                      |       -       |   ❌    |   ❌    |   ✔️    |    ✔️    |

</details>

## 插件配置说明

> 本插件的配置不是必须的, 保持默认配置即可使用大部分功能

ASF.json

```json
{
  //ASF 配置
  "CurrentCulture": "...",
  "IPCPassword": "...",
  "...": "...",
  //ASFTradeExtension 配置
  "ASFEnhance": {
    "EULA": true,
    "Statistic": true,
    "MaxItemPerTrade": 255,
    "CacheTTL": 600
  }
}
```

| 配置项            | 类型   | 默认值 | 说明                                                                                     |
| ----------------- | ------ | ------ | ---------------------------------------------------------------------------------------- |
| `EULA`            | bool   | `true` | 是否同意 [EULA](#EULA)\*                                                                 |
| `Statistic`       | bool   | `true` | 是否允许发送统计数据, 仅用于统计插件用户数量, 不会发送任何其他信息                       |
| `MaxItemPerTrade` | ushort | `255`  | 单个交易最多物品数量, ASF 的默认值是 255, 如果报价中的物品超过此数量会自动拆分成多个报价 |
| `CacheTTL`        | ushort | `600`  | 库存缓存过期时间, 单位秒, 缓存未过期也可使用命令 `RELOADCACHE` 强制刷新缓存              |

## 插件指令说明

### 插件更新

| 命令                | 缩写  | 权限            | 说明                          |
| ------------------- | ----- | --------------- | ----------------------------- |
| `ASFTradeExtension` | `ATE` | `FamilySharing` | 查看 ASFTradeExtension 的版本 |

### 卡牌交易

| 命令                                               | 缩写    | 权限       | 说明                                                             |
| -------------------------------------------------- | ------- | ---------- | ---------------------------------------------------------------- |
| `FULLSETLIST [Bots] [Config]`                      | `FSL`   | `Operator` | 显示普通卡牌套数信息, 可用参数 \[-page 页码\] \[-line 显示行数\] |
| `FULLSETLISTFOIL [Bots] [Config]`                  | `FSLF`  | `Operator` | 显示闪卡卡牌套数信息, 可用参数 \[-page 页码\] \[-line 显示行数\] |
| `FULLSETLISTSALE [Bots]`                           | `FSLS`  | `Operator` | 显示促销卡牌套数信息                                             |
| `FULLSET [Bots] <appIds>`                          | `FS`    | `Operator` | 显示指定 App 的普通卡牌套数信息                                  |
| `FULLSETFOIL [Bots] <appIds>`                      | `FSF`   | `Operator` | 显示指定 App 的闪亮卡牌套数信息                                  |
| `SENDCARDSET [Bots] AppId SetCount TradeLink`      | `SCS`   | `Master`   | 向指定交易链接发送指定`SetCount`套指定`AppId`的普通卡牌          |
| `SENDCARDSETFOIL [Bots] AppId SetCount TradeLink`  | `SCSF`  | `Master`   | 向指定交易链接发送指定`SetCount`套指定`AppId`的闪亮卡牌          |
| `2SENDCARDSET [Bots] AppId SetCount TradeLink`     | `2SCS`  | `Master`   | 同 `SENDCARDSET`, 发送交易后自动确认交易 (需要配置 2FA)          |
| `2SENDCARDSETFOIL [Bots] AppId SetCount TradeLink` | `2SCSF` | `Master`   | 同 `SENDCARDSETFOIL`, 发送交易后自动确认交易 (需要配置 2FA)      |

---

[![Repobeats analytics image](https://repobeats.axiom.co/api/embed/c7bad85b243c7305a5de1fa591469f64125c4048.svg "Repobeats analytics image")](https://github.com/chr233/ASFTradeExtension/pulse)

---

[![Stargazers over time](https://starchart.cc/chr233/ASFTradeExtension.svg)](https://github.com/chr233/ASFTradeExtension/stargazers)

---
