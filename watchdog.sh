#!/bin/bash
while true
do

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
        dotnet build ./ChickenBot.sln

        # Move modules into runtime folder
        echo
        echo
        echo Moving modules...
        echo
        echo
        pushd ChickenBot/bin/Debug/net7.0/plugins
        cp /tmp/config.json.bak ../config.json
        mv ../../../../../ChickenBot.AdminCommands/bin/Debug/net7.0/ChickenBot.AdminCommands.dll ./
        mv ../../../../../ChickenBot.API/bin/Debug/net7.0/ChickenBot.API.dll ./
        mv ../../../../../ChickenBot.AssignableRoles/bin/Debug/net7.0/ChickenBot.AssignableRoles.dll ./
        mv ../../../../../ChickenBot.ChatAI/bin/Debug/net7.0/ChickenBot.ChatAI.dll ./
        mv ../../../../../ChickenBot.FlagGame/bin/Debug/net7.0/ChickenBot.FlagGame.dll ./
        mv ../../../../../ChickenBot.Fun/bin/Debug/net7.0/ChickenBot.Fun.dll ./
        mv ../../../../../ChickenBot.Info/bin/Debug/net7.0/ChickenBot.Info.dll ./
        mv ../../../../../ChickenBot.Petitions/bin/Debug/net7.0/ChickenBot.Petitions.dll ./
        mv ../../../../../ChickenBot.Quotes/bin/Debug/net7.0/ChickenBot.Quotes.dll ./
        mv ../../../../../ChickenBot.VerificationSystem/bin/Debug/net7.0/ChickenBot.VerificationSystem.dll ./
        popd

        echo
        echo
        echo Starting bot...
        echo
        echo
        pushd ChickenBot/bin/Debug/net7.0/
        ./ChickenBot

        # Backup to tmp
        cp config.json /tmp/config.json.bak

        # Reset
        popd
done
