using System;
using UnityEngine;

namespace InsaneMonopoly.Runtime
{
    [Serializable]
    public sealed class InsaneMonopolyCatalog
    {
        public RuleData rules = new RuleData();
        public BoardSpaceData[] spaces = Array.Empty<BoardSpaceData>();
        public CardData[] cards = Array.Empty<CardData>();
    }

    [Serializable]
    public sealed class RuleData
    {
        public string gameName = "Insane Monopoly 3D";
        public int minPlayers = 2;
        public int maxPlayers = 8;
        public int startingCash = 2500;
        public int goSalary = 300;
        public int jailFine = 75;
        public int maxDoublesBeforeJail = 3;
        public bool auctionEnabled = true;
        public bool freeParkingJackpot = true;
        public int upgradeLevels = 5;
    }

    [Serializable]
    public sealed class BoardSpaceData
    {
        public string id = string.Empty;
        public string name = string.Empty;
        public string kind = "property";
        public string set = string.Empty;
        public string color = "#ffffff";
        public string cardDeck = string.Empty;
        public int price;
        public int rent;
        public int amount;
        public string description = string.Empty;
    }

    [Serializable]
    public sealed class CardData
    {
        public string deck = string.Empty;
        public string title = string.Empty;
        public string body = string.Empty;
        public int cashDelta;
        public int moveDelta;
        public string moveToSpaceId = string.Empty;
        public string statusEffect = string.Empty;
    }

    public enum SpaceKind
    {
        Go,
        Property,
        Railroad,
        Utility,
        Tax,
        Card,
        Jail,
        GoToJail,
        FreeParking,
        Special
    }

    public static class SpaceKindParser
    {
        public static SpaceKind Parse(string kind)
        {
            switch ((kind ?? string.Empty).Trim().ToLowerInvariant())
            {
                case "go": return SpaceKind.Go;
                case "railroad": return SpaceKind.Railroad;
                case "utility": return SpaceKind.Utility;
                case "tax": return SpaceKind.Tax;
                case "card": return SpaceKind.Card;
                case "jail": return SpaceKind.Jail;
                case "gotojail": return SpaceKind.GoToJail;
                case "freeparking": return SpaceKind.FreeParking;
                case "special": return SpaceKind.Special;
                default: return SpaceKind.Property;
            }
        }
    }
}
