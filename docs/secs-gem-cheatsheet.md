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

| Code | Type | C# |
|---|---|---|
| `L` | List | — |
| `A` | ASCII text | `string` |
| `B` | Binary (raw byte / flags) | `byte` |
| `U1` / `U2` / `U4` | Unsigned int (1/2/4 bytes) | `byte` / `ushort` / `uint` |
| `I1` / `I2` / `I4` | Signed int (1/2/4 bytes) | `sbyte` / `short` / `int` |
| `Boolean` | True / false | `bool` |

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

### Alarms (ALID)

| ALID | Text |
|---|---|
| 1 | Conveyor jam |
| 2 | Sensor timeout |
| 3 | Vacuum pressure low |
| 99 | Manual fault (test) |

## S5F1 structure

```
<L
  <B ALCD>    alarm code
  <U4 ALID>   alarm ID
  <A ALTX>    alarm text
```

### ALCD (alarm code byte)

- Bit 8 (`0x80`): 1 = alarm **set**, 0 = alarm **cleared**
- Bits 1–7: category — MachinePanel uses **6** (equipment status warning)
- Set = `0x86`, Cleared = `0x06`

## Acknowledge codes

| Reply | Item | 0 = |
|---|---|---|
| S6F12 | `<B ACKC6>` | accepted |
| S5F2 | `<B ACKC5>` | accepted |
