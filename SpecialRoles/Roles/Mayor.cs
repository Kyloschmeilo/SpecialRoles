using SpecialRoles.Core;
using SpecialRoles.Game;
using UnityEngine;

namespace SpecialRoles.Roles;

/// <summary>
/// Bürgermeister (Crew): seine Stimme zählt doppelt. Der Host zählt sie beim Auswerten doppelt (CalculateVotes),
/// alle Clients zeigen dafür ein zweites Stimm-Symbol (PopulateResults).
/// </summary>
internal static class Mayor
{
    /// <summary>Host, Postfix von MeetingHud.CalculateVotes: Stimme des Bürgermeisters ein zweites Mal zählen.</summary>
    public static void AddVotes(MeetingHud meeting, Il2CppSystem.Collections.Generic.Dictionary<byte, int> votes)
    {
        foreach (var area in meeting.playerStates)
        {
            if (area == null || !area.DidVote) continue;
            var voter = area.PlayerId.Value;
            if (RoleState.Get(voter) != RoleId.Mayor || !Players.IsAlive(Players.ById(voter))) continue;
            var target = area.VotedForId.Value;
            if (target == PlayerVoteArea.HasNotVoted || target == PlayerVoteArea.MissedVote || target == PlayerVoteArea.DeadVote) continue;
            votes[target] = (votes.ContainsKey(target) ? votes[target] : 0) + 1;
        }
    }

    /// <summary>Alle Clients, Postfix von MeetingHud.PopulateResults: zweites Symbol für den Bürgermeister.</summary>
    public static void AddVoteIcons(MeetingHud meeting, Il2CppInterop.Runtime.InteropTypes.Arrays.Il2CppStructArray<MeetingHud.VoterState> states)
    {
        foreach (var state in states)
        {
            if (RoleState.Get(state.VoterId) != RoleId.Mayor) continue;
            var voter = GameData.Instance.GetPlayerById(state.VoterId);
            if (voter == null) continue;

            Transform? parent = null;
            var index = 0;
            if (state.SkippedVote)
            {
                parent = meeting.SkippedVoting != null ? meeting.SkippedVoting.transform : null;
                foreach (var other in states)
                    if (other.SkippedVote) index++;
            }
            else
            {
                foreach (var area in meeting.playerStates)
                    if (area != null && area.PlayerId.Value == state.VotedForId)
                        parent = area.transform;
                foreach (var other in states)
                    if (!other.SkippedVote && other.VotedForId == state.VotedForId) index++;
            }
            if (parent != null) meeting.BloopAVoteIcon(voter, index, parent);
        }
    }
}
