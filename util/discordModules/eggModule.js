let debugging = require("../debugging.js");
let discordModule = require("../discordModule.js");
let mongoUtil = require("../mongoUtil.js");
let botConfig = require('../.././config.json');
let e6 = require('../e6.js');

var sweetdreamsSpeedLock = false;
var sweetdreamsLock = false;

//Sweetdreams command, sorry furi 
function sweetdreams(msg){
    //Flip coin
    let coin = Math.floor(Math.random() * 100);

    //"15%" chance of dc
    if (coin >= 75){
        //Get Furi
        let member = msg.guild.members.cache.get('693042484619509760');

        //Disconnects user from vc
        member.voice.setChannel(null);
    }

    messages = ["https://media.discordapp.net/attachments/206875238066028544/970993761691766784/Untitled_Artwork.png", "GO TO BED! <@693042484619509760>", "Bedtime! <@693042484619509760> <:chicken_smile:236628343758389249>", "<:toothless_upright:955240038302613514> <@693042484619509760> *smothers you to sleep with wings*"];

    msg.channel.send(messages[Math.floor(Math.random() * messages.length)]);
}

function processMessage(msg, client, args){
    if ((args[0] === `cluck`) || (args[0] === `bok`) || (args[0] === `bawk`) || (args[0] === `squark`)) {
        replies = ["cluck", "bok", "*tilts head in confusion*", "bawk", "*scratches the ground*", "*pecks you*", "*flaps wings*"]
        msg.reply(replies[Math.floor(Math.random() * replies.length)]);
        return true;
    }

    else if (args[0] === `love`) {
        replies = [
        "*bonk*",
        "*cuddles up next to you*", 
        "<:chicken_smile:236628343758389249> *stares at you for several seconds, before flapping away*",
        "*gives you a small flower*", 
        ":heart:",
        "<:chicken_smile:236628343758389249>"
        ]
        msg.reply(replies[Math.floor(Math.random() * replies.length)]);
        return true;
    }

    else if ((args[0] === `kill`) || (args[0] === `attack`)) {
        //Attack the mentioned user
        let punishedUser = msg.mentions.users.first();

        //Do not attack ourselves
        if (punishedUser.id == client.user.id){
            msg.channel.send(`*Trust nobody, not even yourself*`);
            return true;
        }
        //Do not attack our creator
        else if (punishedUser.id == "102606498860896256"){
            msg.channel.send(`**BRAWK!** *pecks and chases* ${msg.author.username}`);
            return true;
        }
        //Attack mentioned user
        msg.channel.send(`*pecks and chases* ${punishedUser.username}`);
        return true;
    }

    else if ((args[0] === `pet`) || (args[0] === `feed`)) {
        replies = [
        "<:chicken_smile:236628343758389249>",
        "*cuddles up into you*",
        "*swawks, coughing up a half digested piece of corn. Looking up at you expectingly*"
        ]
        msg.reply(replies[Math.floor(Math.random() * replies.length)]);
        return true;
    }
    
    //Bonker
    else if ((args[0] === `e6`) || (args[0] === `lewd`)) {
        msg.reply("*bonk*");
        return true;
    }

    //Ping
    else if ((args[0] === `ping`) || (args[0] === `echo`)){
        var wheel = Math.floor(Math.random() * 100);
        if ((50 <= wheel) && (wheel <= 55)){
            return msg.channel.send("<:toothless_ping:587068355987505155>");
        }
        else{
            return msg.channel.send('Pong!');
        }
    }

    //Sweet Dreams
    else if (args[0] == "sweetdreams"){
        //If it is not the target user
        if (msg.author.id != "693042484619509760"){
            //Get Current Time
            let currentHour = new Date().getUTCHours();

            //Check cooldown is bigger than 2 hours or 1 for paying
            if ((!sweetdreamsLock || (!sweetdreamsSpeedLock && msg.author.id == "255121046607233025")) || (msg.author.id == "102606498860896256")){
                //Check if it is between 10pm-6am UTC
                if (((currentHour >= 21) && (currentHour > 12)) || ((currentHour < 7) && (currentHour >= 0))) {
                    
                    if (!sweetdreamsSpeedLock && msg.author.id == "255121046607233025"){

                        //Send Message
                        sweetdreams(msg);

                        //Locks
                        sweetdreamsSpeedLock = true;

                        //Reset Speed Lock
                        setTimeout(() => {
                            sweetdreamsSpeedLock = false;
                        }, 60*60*1000);
                    }
                    else if (msg.author.id == "102606498860896256"){
                        //Send Message
                        sweetdreams(msg);
                    }
                    else if (msg.author.id != "255121046607233025"){

                        //Send Message
                        sweetdreams(msg);

                        //Locks
                        sweetdreamsLock = true;

                        //Reset Lock
                        setTimeout(() => {
                            sweetdreamsLock = false;
                        }, 2*60*60*1000);
                    }
                }
                else{
                    msg.reply("It is not bedtime for furious");
                }
            }
            else{
                msg.reply("`Command is on cooldown, try again later`")
            }

            return true;
        }
        else{
            if (!sweetdreamsLock){
                messages = ["<:toothless_upright:955240038302613514> *goodnight*", "*stares patiently*", "*bawk*"];
                msg.reply(messages[Math.floor(Math.random() * messages.length)]);
                return true;
            }
            else{
                msg.reply("`Command is on cooldown, try again later`")
            }
        }
    }

    return false;
}

//Exports
module.exports.processMessage = processMessage;