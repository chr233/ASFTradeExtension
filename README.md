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
[![爱发电](https://img.shields.io/badge/爱发电-chr__-ea4aaa.svg?logo=github-sponsors)](https://afdian.com/@chr233)

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
| `Statistic`       | bool   | `true` | 是否允许发送统计数据, 仅用于统计插件用户数量, 不会发送任何其他信息                       |
| `MaxItemPerTrade` | ushort | `255`  | 单个交易最多物品数量, ASF 的默认值是 255, 如果报价中的物品超过此数量会自动拆分成多个报价 |
| `CacheTTL`        | ushort | `600`  | 库存缓存过期时间, 单位秒, 缓存未过期也可使用命令 `RELOADCACHE` 强制刷新缓存              |

## 插件指令说明

| 命令                | 缩写  | 权限            | 说明                          |
| ------------------- | ----- | --------------- | ----------------------------- |
| `ASFTradeExtension` | `ATE` | `FamilySharing` | 查看 ASFTradeExtension 的版本 |

### 插件命令

#### 查询命令

| 命令                          | 缩写   | 权限     | 说明                                                                    |
| ----------------------------- | ------ | -------- | ----------------------------------------------------------------------- |
| `GETMASTERBOT`                | `GM`   | `Master` | 获取发货 Bot                                                            |
| `SETMASTERBOT [bot]`          | `SM`   | `Master` | 设置发货 Bot                                                            |
| `FULLSETLIST [Config]`        | `FSL`  | `Master` | 显示普通卡牌套数信息, 可用参数 \[-page 页码\] \[-line 显示行数\]        |
| `FULLSETLISTFOIL [Config]`    | `FSLF` | `Master` | 显示闪卡卡牌套数信息, 可用参数 \[-page 页码\] \[-line 显示行数\]        |
| `FULLSETLISTSALE`             | `FSLS` | `Master` | 显示促销卡牌套数信息                                                    |
| `FULLSET [Bots] <appIds>`     | `FS`   | `Master` | 显示指定 App 的普通卡牌套数信息                                         |
| `FULLSETFOIL [Bots] <appIds>` | `FSF`  | `Master` | 显示指定 App 的闪亮卡牌套数信息                                         |
| `GEMSINFO [Bots]`             | `GI`   | `Master` | 显示指定机器人的宝珠库存数量                                            |
| `RELOADCACHE [Bots]`          |        | `Master` | 刷新库存缓存                                                            |
| `CLEARCACHE [Bots]`           |        | `Master` | 清除交易中物品的记录 (如果有未接受的报价可能会导致同一个物品被重复发货) |

#### 交易命令

| 命令                                          | 缩写    | 权限     | 说明                                                                       |
| --------------------------------------------- | ------- | -------- | -------------------------------------------------------------------------- |
| `SENDCARDSET AppId SetCount TradeLink`        | `SCS`   | `Master` | 向指定交易链接发送指定`SetCount`套指定`AppId`的普通卡牌                    |
| `SENDCARDSETBOT AppId SetCount TargetBot`     | `SCSB`  | `Master` | 向指定机器人发送指定`SetCount`套指定`AppId`的普通卡牌                      |
| -                                             | -       | -        | -                                                                          |
| `SENDCARDSETFOIL AppId SetCount TradeLink`    | `SCSF`  | `Master` | 向指定交易链接发送指定`SetCount`套指定`AppId`的闪亮卡牌                    |
| `SENDCARDSETBOTFOIL AppId SetCount TargetBot` | `SCSBF` | `Master` | 向指定机器人发送指定`SetCount`套指定`AppId`的闪亮卡牌                      |
| -                                             | -       | -        | -                                                                          |
| `SENDGEMS GemCount TradeLink`                 | `SG`    | `Master` | 向指定交易链接发送指定数量的宝珠, 宝珠袋按照 1000 宝珠计算, 优先使用宝珠袋 |
| `SENDGEMSBOT GemCount TargetBot`              | `SGB`   | `Master` | 向指定机器人发送指定数量的宝珠, 宝珠袋按照 1000 宝珠计算, 优先使用宝珠袋   |
| -                                             | -       | -        | -                                                                          |
| `SENDLEVELUP Level TradeLink`                 | `SLU`   | `Master` | 向指定交易链接发送能升级到指定等级的卡牌套数                               |
| `SENDLEVELUPSET SetCount TradeLink`           | `SLUS`  | `Master` | 向指定交易链接发送指定套数的卡牌                                           |

> 本组命令添加 `2` 前缀可以自动确认报价, 例如 `2SCS`, `2SCSF`, `2SCSB`, `2SCSBF` 等

---

[![Repobeats analytics image](https://repobeats.axiom.co/api/embed/c7bad85b243c7305a5de1fa591469f64125c4048.svg "Repobeats analytics image")](https://github.com/chr233/ASFTradeExtension/pulse)

---

[![Stargazers over time](https://starchart.cc/chr233/ASFTradeExtension.svg)](https://github.com/chr233/ASFTradeExtension/stargazers)

---
