## Table of Contents

- [Sponsors](#sponsors)
- [Features](#features)
- [Configuration](#configuration)

## Sponsor this project

[![patreon](https://i.imgur.com/u6aAqeL.png)](https://www.patreon.com/join/4865914)  [![ko-fi](https://ko-fi.com/img/githubbutton_sm.svg)](https://ko-fi.com/zfolmt)

## Sponsors

Jairon Orellana; Odjit; Jera; Eve winters;

## Features

Automatically do the following on a configurable timer after loading the main menu, mouse left-click will stop the timer until next restart (auto hosting WIP):
- Continue last played
- Join by address
 
## Configuration

### General

- **Enabled**: `Enabled` (bool, default: false)  
  Enable or disable mod functions (enables base auto continue functionality, needs to be set to true for autoJoin and autoHost).
- **Timer Seconds**: `TimerSeconds` (int, default: 5)  
  Set the timer in seconds before continuing or joining.
- **IPAddress**: `IPAddress` (string, default: "")  
  Paste IP followed by colon then port here like '102.3.4.1:9876', just as if you were using direct connect (no quotes though).
- **Auto Join**: `AutoJoin` (bool, default: false)  
  Enable to join server at entered address instead of continuing if auto join is enabled.


