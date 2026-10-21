# SECS/GEM Cheat Sheet

Quick reference for the SECS/GEM messages and IDs used in MachinePanel.

## The rule

Messages are named **SxFy** (Stream x, Function y).
An **odd** function is a **request**; the **next even** function is its **reply** (e.g. S1F1 → S1F2).

## Messages

| Message | Direction | Meaning |
|---|---|---|
| **S1F1** | Host → Equipment | Are you there? |
| **S1F2** | Equipment → Host | Yes — model name + software revision |
| **S6F11** | Equipment → Host | Event report — something happened, with data |
| **S6F12** | Host → Equipment | Event report acknowledged |
| **S5F1** | Equipment → Host | Alarm report |
| **S5F2** | Host → Equipment | Alarm acknowledged |
| **S2F41** | Host → Equipment | Host command (e.g. START, STOP) |
| **S2F42** | Equipment → Host | Command accepted / rejected |

## Streams

| Stream | Area |
|---|---|
| S1 | Equipment status / communication |
| S2 | Equipment control |
| S5 | Alarms |
| S6 | Data collection / events |

## Data item types

| Code | Type |
|---|---|
| `L` | List |
| `A` | ASCII text |
| `U4` | 4-byte unsigned integer |

## MachinePanel IDs

### Collection events (CEID)

| CEID | Name | Report(s) |
|---|---|---|
| 1000 | MachineStateChanged | RPTID 1 |

### Reports (RPTID)

| RPTID | Contents |
|---|---|
| 1 | `<A>` machine state, `<U4>` part count |

## S6F11 structure

```
<L
  <U4 DATAID>
  <U4 CEID>
  <L                    reports
    <L
      <U4 RPTID>
      <L values...>
```

## Connection (HSMS)

- **Equipment** (MachinePanel) is **passive** — listens on `127.0.0.1:5000`.
- **Host** (FakeHost) is **active** — connects to the equipment.
- States: `Connecting` → `Connected` → `Selected` (ready for messages).
