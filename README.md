![PepperDash Logo](/images/logo_pdt_no_tagline_600.png)

# Acuity Fresco Lighting Plugin

## Device Specific Information

Update the below readme as needed to support documentation of the plugin

### Communication Settings

Update the communication settings as needed for the plugin being developed.

| Setting      | Value   |
| ------------ | ------- |
| Baud rate    | 115,200 |
| Data bits    | 8       |
| Stop bits    | 1       |
| Parity       | None    |
| Flow Control | None    |
| Delimiter    | "\n"    |

#### Plugin Valid Communication methods

```c#
Com
```

#### Example API Commands

API Command

```c#
'scene {sceneId} {level} 0 {roomId}'
```

`sceneId` valid values are 0-36, `sceneId` 0 represents off
`level` valid values are 0-100, values < 100 will be applied relative to the current lighting level value
`roomId` valid values are A-X, `roomId`'s can be concatenated, for example `ABC` is avalid room ID for combined spaces


### Plugin Configuration Object

Update the configuration object as needed for the plugin being developed.

```json
{
 "devices": [
  {
    "key": "lights-1",
    "name": "Acuity Fresco Lighting Scenes",
    "type": "acuityfresco",
    "group": "pluginDevices",
    "properties": {
     "control": {
      "method": "com",
      "controlPortDevKey": "processor",
      "controlPortNumber": 1,
      "comParams": {
       "baudRate": 115200,
       "dataBits": 8,
       "stopBits": 1,
       "parity": "None",
       "protocol": "RS232",
       "hardwareHandshake": "None",
       "softwareHandshake": "None"
      }
     },
     "pollTimeMs": 30000,
     "warningTimeoutMs": 180000,
     "errorTimeoutMs": 300000,
     "scenes": [
      {
       "name": "Scene 1",
       "id": 1,
       "roomId": "A",
       "level": 100
      },
      {
       "name": "Scene 2",
       "id": 5,
       "roomId": "B",
       "level": 50
      },
      {
       "name": "Scene 3",
       "id": 36,
       "roomId": "X",
       "level": 0
      }
     ]
    }
   }  
 ]
}
```

### Plugin Bridge Configuration Object

Update the bridge configuration object as needed for the plugin being developed.

```json
{
 "devices": [
  {
    "key": "lights-1-bridge",
    "uid": 11,
    "name": "Example Plugin Bridge",
    "group": "api",
    "type": "eiscApiAdvanced",
    "properties": {
     "control": {
      "tcpSshProperties": {
       "address": "127.0.0.2",
       "port": 0
      },
      "ipid": "B1"
     },
     "devices": [
      {
       "deviceKey": "lights-1",
       "joinStart": 1
      }
     ]
    }
   }
 ]
}
```

### SiMPL EISC Bridge Map

#### Digitals

| dig-o (Input/Triggers) | I/O | dig-i (Feedback)          |
| ---------------------- | --- | ------------------------- |
|                        | 1   | Is Online                 |
| Scene 1 Select         | 11  | Scene 1 Feedback          |
| Scene 2 Select         | 12  | Scene 2 Feedback          |
| Scene 3 Select         | 13  | Scene 3 Feedback          |
| Scene 4 Select         | 14  | Scene 4 Feedback          |
| Scene 5 Select         | 15  | Scene 5 Feedback          |
| Scene 6 Select         | 16  | Scene 6 Feedback          |
| Scene 7 Select         | 17  | Scene 7 Feedback          |
| Scene 8 Select         | 18  | Scene 8 Feedback          |
| Scene 9 Select         | 19  | Scene 9 Feedback          |
| Scene 10 Select        | 20  | Scene 10 Feedback         |
|                        | 41  | Scene 1 Visible Feedback  |
|                        | 42  | Scene 2 Visible Feedback  |
|                        | 43  | Scene 3 Visible Feedback  |
|                        | 44  | Scene 4 Visible Feedback  |
|                        | 45  | Scene 5 Visible Feedback  |
|                        | 46  | Scene 6 Visible Feedback  |
|                        | 47  | Scene 7 Visible Feedback  |
|                        | 48  | Scene 8 Visible Feedback  |
|                        | 49  | Scene 9 Visible Feedback  |
|                        | 50  | Scene 10 Visible Feedback |

#### Analogs

| an_o (Input/Triggers) | I/O | an_i (Feedback)                       |
| --------------------- | --- | ------------------------------------- |
|                       |     | Communication Monitor Status Feedback |
|                       |     | Socket Status Feedback                |
| Select Scene by Index | 10  | Scene Index Feedback                  |

#### Serials

| serial-o (Input/Triggers) | I/O | serial-i (Feedback) |
| ------------------------- | --- | ------------------- |
|                           | 1   | Device Name         |

### DEVJSON Commands

```plaintext
devjson:1 {"deviceKey":"lights-1", "methodName":"GetScenes", "params":[]}

devjson:1 {"deviceKey":"lights-1", "methodName":"ResetDebugLevels", "params":[]}
devjson:1 {"deviceKey":"lights-1", "methodName":"SetDebugLevels", "params":[{level 0-2}]} 
```
