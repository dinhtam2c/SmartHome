# Data Formats

## 1. Gateway provision request (gateway to server)
```json
{
    "key": "abcxyz",
    "mac": "01:02:03:04:05:06",
    "name": "gw",
    "manufacturer": null,
    "model": null,
    "firmwareVersion": "1.0",
    "timestamp": 1762621085
}
```

## 2. Gateway provision response (server to gateway)
```json
{
    "gatewayId": "uuid_here"
    // TODO: add credentials
}
```

## 3. Device provision request (device to gateway)
```json
{
    "key": "123456",
    "identifier": "serial-number-or-something-unique",
    "name": "dev1ce",
    "manufacturer": null,
    "model": null,
    "firmwareVersion": "1.0",
    "timestamp": 1762621085,

    "sensors": [
        {
            "name": "DHT22",
            "type": "Temperature",
            "unit": "C",
            "min": -40,
            "max": 80,
            "accuracy": 0.5
        },
        {
            "name": "DHT22",
            "type": "Humidity",
            "unit": "%",
            "min": 0,
            "max": 100,
            "accuracy": 5
        }
    ],

    "actuators": [
        {
            "name": "Quat pro max",
            "type": "fan",
            "states": [
                "power",
                "speed"
            ],
            "commands": [
                "turn_on",
                "turn_off",
                "set_speed"
            ]
        }
    ]
}
```

## 4. Device provision request (gateway to server)
```json
{
    "gatewayId": "GW1",
    // ... (see 3.)
}
```

## 5. Device provision response (server to gateway)
```json
{
    "deviceIdentifier": "sn",
    "deviceId": "uuid_here",
    "sensorIds": ["s1", "s2"],
    "actuatorIds": ["a1"]
    // TODO: add credentials
}
```

## 6. Device offline (gateway to server)
```json
{
    "gatewayId": "GW1",
    "timestamp": 1762621085456,
    "deviceId": "D1"
}
```

## 7. Device data (device to gateway)
```json
{
    "deviceId": "D1",
    "timestamp": 1762621085,
    "priority": "LOW",
    "data": [
        {
            "sensorId": "S1",
            "value": 25.5
        },
        {
            "sensorId": "S2",
            "value": 60.5
        }
    ],

    // "status": {
    //     "battery": 95,
    //     "uptime": 1234,
    //     "error": null

    // },
    // "actuators": [
    //     {
    //         "id": "A1",
    //         "state": [
    //             {
    //                 "power": "on",
    //                 "speed": "50"
    //             }
    //         ],
    //     }
    // ]
}
```

## 8. Gateway data (gateway to server)
```json
{
    "gatewayId": "GW1",
    "timestamp": 1762621086,
    "data": [
        // See Device data
    ]

    // "status": {
    //     "connected_devices": 1,
    //     "battery": 100,
    //     "uptime": 2468,
    //     "error": null
    // },
}
```

## 9. Device command (server to gateway to device)
```json
{
    "deviceId": "D1",
    "actuatorId": "A1",
    "command": "TurnOn",
    "parameters": null,
}
```

## 10. New rule (server to gateway)
```json
{
    "gatewayId": "GW1",
    "ruleId": "R1",
    "enabled": true,
    "timestamp": 1762621086000,
    "conditions": [
        {
            "id": "A",
            "deviceId": "D1",
            "sensorId": "S1",
            "operator": ">",
            "value": 36
        },
        {
            "id": "B",
            "deviceId": "D1",
            "actuatorId": "A1",
            "state": "Power",
            "operator": "==",
            "value": "Off"
        }
    ],
    "logic": "A AND B",
    "actions": [
        {
            "deviceId": "D1",
            "actuatorId": "A1",
            "command": "TurnOn",
            "parameters": null
        },
        {
            "deviceId": "D1",
            "actuatorId": "A1",
            "command": "SetSpeed",
            "parameters": [100]
        }
    ]
}
```
