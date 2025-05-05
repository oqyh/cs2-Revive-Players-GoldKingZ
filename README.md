## .:[ Join Our Discord For Support ]:.

<a href="https://discord.com/invite/U7AuQhu"><img src="https://discord.com/api/guilds/651838917687115806/widget.png?style=banner2"></a>

# [CS2] Revive-Players-GoldKingZ (1.0.0)

Allow To Revive Players With Flags


![revive](https://github.com/user-attachments/assets/f5b18eb4-b913-4098-9668-4e76ebdb3062)


---

## 📦 Dependencies
[![Metamod:Source](https://img.shields.io/badge/Metamod:Source-2d2d2d?logo=sourceengine)](https://www.sourcemm.net)

[![CounterStrikeSharp](https://img.shields.io/badge/CounterStrikeSharp-83358F)](https://github.com/roflmuffin/CounterStrikeSharp)

[![JSON](https://img.shields.io/badge/JSON-000000?logo=json)](https://www.newtonsoft.com/json) [Included in zip]


---

## 📥 Installation

### Plugin Installation
1. Download the latest `Revive-Players-GoldKingZ.x.x.x.zip` release
2. Extract contents to your `csgo` directory
3. Configure settings in `Revive-Players-GoldKingZ/config/config.json`
4. Restart your server

---

## 🛠️ `config.json`

<details open>
<summary><b>Main Config</b> (Click to expand 🔽)</summary>

| Property | Description | Values | Required |
|:---------|:------------|:-------|:---------|
| `Revive_Flags` | Access control for revive | SteamIDs/Flags/Groups (e.g., `"SteamID: 123... \| Flag: @vip"`), `""` = Everyone | - |
| `Revive_Limit` | Maximum revives allowed | Number (`0` = Unlimited) | - |
| `Revive_Limit_Mode` | Limit calculation method | `1` = Team-based, `2` = Per-player | - |
| `Revive_Limit_Reset` | Reset revive limits | `true` = Round start, `false` = Map change | - |
| `Revive_Cost` | Revive resource cost | Number (`0` = Free) | - |
| `Revive_Cost_Mode` | Cost payment type | `1` = Money, `2` = Health | `Revive_Cost > 0` |
| `Revive_CoolDown_After_Revive` | Cooldown between revives | Seconds (`0` = Disabled) | - |
| `Revive_Immunity_From_Cooldown_Flags` | Cooldown immunity | SteamIDs/Flags/Groups | `Revive_CoolDown_After_Revive > 0` |
| `Revive_Distance` | Maximum revive distance | Number (units) | - |
| `Revive_Duration` | Time to hold USE | Seconds | - |
| `Revive_Health` | Health after revival | Number (1-100) | - |
| `Revive_CancelRevivingOnAdditionalInput` | Cancel on extra input | `true`/`false` | - |
| `Revive_BlockDamageOnReviving` | Damage protection | `true`/`false` | - |
| `Revive_AllowRevivingTeam` | Allowed teams | `0` = Any, `1` = CT, `2` = T | - |
| `Revive_ModelAnimation` | Revive animation model | File path, `""` = Disabled | - |
| `Revive_NameAnimation` | Animation name | String, `""` = Disabled | - |
| `Revive_FreezeOnReviving` | Freeze during revive | `true`/`false` | - |
| `Revive_DontUnFreezeIfPlayerWasFreezed` | Preserve original freeze | `true`/`false` | - |
| `DeadBody_Arrow` | Show indicator arrow | `true`/`false` | - |
| `DeadBody_Arrow_Color` | Arrow color | RGBA values (e.g., `"120,245,27,0.45"`) | `DeadBody_Arrow=true` |
| `DeadBody_MoveArrow_From_Ground_By` | Arrow height offset | Number (units) | `DeadBody_Arrow=true` |
| `DeadBody_CircleRadius` | Show circle radius | `true`/`false` | - |
| `DeadBody_CircleRadius_Color` | Circle color | RGBA values | `DeadBody_CircleRadius=true` |
| `DeadBody_MoveCircleRadius_From_Ground_By` | Circle height offset | Number (units) | `DeadBody_CircleRadius=true` |
| `DeadBody_PlayerNameText` | Show name text | `true`/`false` | - |
| `DeadBody_PlayerNameText_FontSize` | Name text size | Number | `DeadBody_PlayerNameText=true` |
| `DeadBody_PlayerNameText_WorldUnitsPerPx` | Text scaling | Number (`0` = Auto) | `DeadBody_PlayerNameText=true` |
| `DeadBody_PlayerNameText_FontName` | Font style | Font name (e.g., `"Tahoma Bold"`) | `DeadBody_PlayerNameText=true` |
| `DeadBody_PlayerNameText_FontColor` | Text color | RGBA values | `DeadBody_PlayerNameText=true` |
| `DeadBody_MovePlayerNameText_From_Ground_By` | Text height offset | Number (units) | `DeadBody_PlayerNameText=true` |

---
</details>


<details>
<summary><b>Utilities Config</b> (Click to expand 🔽)</summary>

| Property | Description | Values | Required |  
|----------|-------------|--------|----------|
| `EnableDebug` | Debug Mode | `true`-Enable<br>`false`-Disable | - |

</details>

---


## 📜 Changelog

<details>
<summary><b>📋 View Version History</b> (Click to expand 🔽)</summary>

### [1.0.0]
- Initial plugin release

</details>

---
