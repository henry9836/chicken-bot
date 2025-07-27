#!/bin/bash
export HOME=/opt/chickenbot/tmpdotnethome
export chicken_plugin_config=plugins.cfg

mkdir -p $HOME

# Update from git
echo
echo
echo Fetching Changes...
echo
echo
git fetch --all
git pull

# Compile the bot
echo
echo
echo Compiling Bot...
echo
echo
dotnet clean ./ChickenBot.sln
dotnet restore ./ChickenBot.sln
dotnet build ./ChickenBot.sln

# Start the bot
echo
echo
echo Starting bot...
echo
echo
pushd ChickenBot/bin/Debug/net9.0/
./ChickenBot

# Backup to tmp
cp config.json ../../../../../config.json.runtime.bak

#Reset
popd
