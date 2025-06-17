# Agents Documentation

## Purpose
Define all automated trading strategies ("agents") within this repository for Codex and contributors.

## Agents Overview

### 1. EMACompetitionBeast.cs
- **Type**: Strategy
- **Description**: Competes various EMA touch strategies to determine optimal entry logic during trend continuation.
- **Inputs**: 
  - EMA Lengths
  - Slope filters
  - Pullback detection
- **Outputs**:
  - Long/short entries
  - Strategy performance metrics
- **Dependencies**:
  - `Indicators/EMACustom.cs`
  - `Utils/Logger.cs`

### 2. VolumeMomentumSniper.cs
- **Type**: Strategy
- **Description**: Uses volume/momentum thresholds to confirm pullbacks and breakout entries.
- **Inputs**: 
  - Volume filter
  - RSI/ATR thresholds
- **Outputs**:
  - Sniper entries with tight stops
- **Dependencies**:
  - `Indicators/VolumeSpike.cs`

## Shared Components
- `Utils/Logger.cs`: Simple console/file logging wrapper.
- `Enums/TradeState.cs`: Enum used across all agents.
- `Interfaces/IStrategyAgent.cs`: Optional interface for standardizing behavior.

## Notes
- All agents adhere to NinjaTrader 8's `Strategy` class model.
- Must be compatible with backtesting and live trading in NinjaTrader's Strategy Analyzer and chart window.

