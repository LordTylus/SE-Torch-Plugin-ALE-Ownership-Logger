using Sandbox.Game.World;
using VRage.Game.ModAPI;

namespace ALE_Ownership_Logger.Utils
{
    class PlayerUtils
    {

        public static bool isOnline(long playerId) {
            return MySession.Static.Players.IsPlayerOnline(playerId);
        }

        public static MyIdentity GetIdentityById(long playerId) {

            foreach (var identity in MySession.Static.Players.GetAllIdentities())
                if (identity.IdentityId == playerId)
                    return identity;

            return null;
        }

        public static string GetPlayerNameById(long playerId) {

            MyIdentity identity = GetIdentityById(playerId);

            if (identity != null)
                return identity.DisplayName;

            return "Nobody";
        }

        public static string GetFactionTagStringForPlayer(long playerId) {

            IMyFaction faction = GetFactionForPlayer(playerId);

            string factionString = "";
            if (faction != null)
                factionString = "[" + faction.Tag + "]";

            return factionString;
        }

        public static IMyFaction GetFactionForPlayer(long playerId) {
            return MySession.Static.Factions.TryGetPlayerFaction(playerId);
        }
    }
}
