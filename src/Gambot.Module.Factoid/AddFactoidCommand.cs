﻿using System;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Gambot.Core;
using Gambot.Data;

namespace Gambot.Module.Factoid
{
    public class AddFactoidCommand : ICommand
    {
        private readonly IDataStoreProvider _dataStoreProvider;

        public AddFactoidCommand(IDataStoreProvider dataStoreProvider)
        {
            _dataStoreProvider = dataStoreProvider;
        }

        public async Task<Response> Handle(Message message)
        {
            if (!message.Addressed)
                return null;

            var match = Regex.Match(message.Text, @"^(.+) (<[^!@>]+>) (.+)$");
            if (!match.Success)
                return null;
            var dataStore = await _dataStoreProvider.GetDataStore("Factoids");

            var trigger = match.Groups[1].Value.ToLowerInvariant();
            var verb = match.Groups[2].Value.ToLowerInvariant();

            var percentMatch = Regex.Match(verb, @"\|(?!.*\|)(.*?)%>$");
            if (percentMatch.Success)
            {
                decimal chanceToTrigger;
                if (!Decimal.TryParse(percentMatch.Groups[1].Value, out chanceToTrigger))
                    return message.Respond($"Sorry, {message.From.Mention}, but {verb} is not properly formatted.");

                if (chanceToTrigger <= 0 || chanceToTrigger > 100)
                    return message.Respond($"Sorry, {message.From.Mention}, but {verb} does not contain a valid percentage.");
            }

            var response = match.Groups[3].Value;

            if (verb == "<alias>" && String.Compare(trigger, response, true) == 0)
                return message.Respond($"Sorry, {message.From.Mention}, but you can't alias {trigger} to itself.");

            var added = await dataStore.Add(trigger, $"{verb} {response}");

            if (!added)
                return message.Respond($"I already knew that, {message.From.Mention}!");
            return message.Respond($"Ok, {message.From.Mention}.");
        }
    }
}