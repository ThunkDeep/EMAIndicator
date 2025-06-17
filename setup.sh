#!/bin/bash
set -e

# Install .NET 8 SDK if not present
if ! command -v dotnet &> /dev/null; then
  echo "Installing .NET 8 SDK..."
  wget https://dot.net/v1/dotnet-install.sh -O dotnet-install.sh
  chmod +x dotnet-install.sh
  ./dotnet-install.sh --channel 8.0
  export PATH="$HOME/.dotnet:$PATH"
fi

# (Optional) Copy NinjaTrader DLLs if you have them in the repo
# cp libs/*.dll .

# Restore dependencies
dotnet restore

echo "Setup complete!"