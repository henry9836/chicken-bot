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
        pushd ChickenBot/bin/Debug/net8.0/plugins
        cp ~/config.json.runtime.bak ../config.json
        mv ../../../../../ChickenBot.AdminCommands/bin/Debug/net8.0/ChickenBot.AdminCommands.dll ./
        mv ../../../../../ChickenBot.API/bin/Debug/net8.0/ChickenBot.API.dll ./
        mv ../../../../../ChickenBot.AssignableRoles/bin/Debug/net8.0/ChickenBot.AssignableRoles.dll ./
        mv ../../../../../ChickenBot.ChatAI/bin/Debug/net8.0/ChickenBot.ChatAI.dll ./
        mv ../../../../../ChickenBot.FlagGame/bin/Debug/net8.0/ChickenBot.FlagGame.dll ./
        mv ../../../../../ChickenBot.Fun/bin/Debug/net8.0/ChickenBot.Fun.dll ./
        mv ../../../../../ChickenBot.Info/bin/Debug/net8.0/ChickenBot.Info.dll ./
        mv ../../../../../ChickenBot.Music/bin/Debug/net8.0/ChickenBot.Music.dll ./
        mv ../../../../../ChickenBot.Petitions/bin/Debug/net8.0/ChickenBot.Petitions.dll ./
        mv ../../../../../ChickenBot.Quotes/bin/Debug/net8.0/ChickenBot.Quotes.dll ./
        mv ../../../../../ChickenBot.ReverseSearch/bin/Debug/net8.0/ChickenBot.ReverseSearch.dll ./
        mv ../../../../../ChickenBot.VerificationSystem/bin/Debug/net8.0/ChickenBot.VerificationSystem.dll ./
        popd

        echo
        echo
        echo Starting bot...
        echo
        echo
        pushd ChickenBot/bin/Debug/net8.0/
        ./ChickenBot

        # Backup to tmp
        cp config.json ~/config.json.runtime.bak

        # Reset
        popd
done
