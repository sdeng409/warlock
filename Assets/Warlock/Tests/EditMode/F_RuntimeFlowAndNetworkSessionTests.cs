using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Warlock.Mvp;

namespace Warlock.Mvp.Tests.EditMode
{
    public class F_RuntimeFlowAndNetworkSessionTests
    {
        [Test]
        public void RoundTransitionPresenter_ReturnsDeterministicUiContract()
        {
            var waiting = RoundTransitionPresenter.Present(RoundState.Waiting, 0, 5);
            Assert.That(waiting.Title, Is.EqualTo("Waiting For Players"));
            Assert.That(waiting.PrimaryAction, Is.EqualTo("HostStartMatchWhenReady"));

            var shop = RoundTransitionPresenter.Present(RoundState.Shop, 2, 5);
            Assert.That(shop.Title, Is.EqualTo("Shop"));
            Assert.That(shop.RoundText, Is.EqualTo("Round 2/5"));
        }

        [Test]
        public void InputBindingRules_Enforce12UniqueBindings()
        {
            var defaults = InputBindingRules.CreateDefaultSlotBindings();
            var valid = InputBindingRules.Validate(defaults);
            Assert.That(valid.Ok, Is.True);
            Assert.That(defaults.Count, Is.EqualTo(12));

            var duplicate = InputBindingRules.Rebind(defaults, "W", "Q");
            Assert.That(duplicate.ok, Is.False);
            Assert.That(duplicate.reason, Is.EqualTo("DUPLICATE_KEY_BINDING"));
        }

        [Test]
        public void LoadoutUiModel_SupportsEquipAndUnequipFlow()
        {
            var owned = new HashSet<string> { "S01", "S02" };
            var ui = new LoadoutUiModel(owned, LoadoutRules.CreateEmptyLoadout());

            var equipQ = ui.Equip("Q", "S01");
            Assert.That(equipQ.Ok, Is.True);

            var duplicate = ui.Equip("W", "S01");
            Assert.That(duplicate.Ok, Is.False);
            Assert.That(duplicate.Reason, Is.EqualTo("DUPLICATE_SKILL_NOT_ALLOWED"));

            var unequipQ = ui.Unequip("Q");
            Assert.That(unequipQ.Ok, Is.True);
            Assert.That(ui.Snapshot()["Q"], Is.Null);
        }

        [Test]
        public void HostRelaySession_CoversRoomInviteSyncAndReconnectPolicy()
        {
            var session = new HostRelaySession("host-1");
            var preRoomSync = session.SyncEvent("RoundStarted", new Dictionary<string, object> { { "round", 1 } });
            Assert.That(preRoomSync.ok, Is.False);
            Assert.That(preRoomSync.reason, Is.EqualTo("ROOM_NOT_CREATED"));

            var created = session.CreateRoom(4, 5);
            Assert.That(created.ok, Is.True);
            Assert.That(session.RoomId, Is.EqualTo("room-0001"));
            Assert.That(session.InviteCode, Is.EqualTo("WLK0001"));

            var unauthorizedJoin = session.JoinPlayer("p2", "intruder");
            Assert.That(unauthorizedJoin.ok, Is.False);
            Assert.That(unauthorizedJoin.reason, Is.EqualTo("UNAUTHORIZED"));

            var missingActorJoin = session.JoinPlayer("p2");
            Assert.That(missingActorJoin.ok, Is.False);
            Assert.That(missingActorJoin.reason, Is.EqualTo("MISSING_ACTOR_ID"));

            var joined = session.JoinPlayer("p2", "host-1");
            Assert.That(joined.ok, Is.True);

            var unauthorizedSync = session.SyncEvent("RoundStarted", new Dictionary<string, object> { { "round", 1 } }, "p2");
            Assert.That(unauthorizedSync.ok, Is.False);
            Assert.That(unauthorizedSync.reason, Is.EqualTo("HOST_ONLY_ACTION"));

            var missingActorSync = session.SyncEvent("RoundStarted", new Dictionary<string, object> { { "round", 1 } });
            Assert.That(missingActorSync.ok, Is.False);
            Assert.That(missingActorSync.reason, Is.EqualTo("MISSING_ACTOR_ID"));

            var synced = session.SyncEvent("RoundStarted", new Dictionary<string, object> { { "round", 1 } }, "host-1");
            Assert.That(synced.ok, Is.True);
            Assert.That(synced.@event!.Value.Seq, Is.GreaterThan(0));

            var feed = session.ReadEventsSince(0);
            Assert.That(feed.Count, Is.EqualTo(3));
            Assert.That(feed[0].Type, Is.EqualTo("RoomCreated"));
            Assert.That(feed[1].Type, Is.EqualTo("PlayerJoined"));
            Assert.That(feed[2].Type, Is.EqualTo("RoundStarted"));

            var unauthorizedHostDisconnect = session.OnDisconnect("host-1", "p2");
            Assert.That(unauthorizedHostDisconnect.ok, Is.False);
            Assert.That(unauthorizedHostDisconnect.action, Is.EqualTo("HOST_ONLY_ACTION"));

            var missingActorDisconnect = session.OnDisconnect("p2");
            Assert.That(missingActorDisconnect.ok, Is.False);
            Assert.That(missingActorDisconnect.action, Is.EqualTo("MISSING_ACTOR_ID"));

            var playerDisconnect = session.OnDisconnect("p2", "p2");
            Assert.That(playerDisconnect.ok, Is.True);
            Assert.That(playerDisconnect.action, Is.EqualTo("MARK_INACTIVE"));

            var missingActorReconnect = session.OnReconnect("p2");
            Assert.That(missingActorReconnect.ok, Is.False);
            Assert.That(missingActorReconnect.action, Is.EqualTo("MISSING_ACTOR_ID"));

            var lastSeqBeforeReconnect = session.ReadEventsSince(0).Last().Seq;
            var reconnect = session.OnReconnect("p2", "host-1");
            Assert.That(reconnect.ok, Is.True);
            Assert.That(reconnect.action, Is.EqualTo("RESUME_FROM_LAST_SEQUENCE"));
            Assert.That(reconnect.resumeFromSeq, Is.EqualTo(lastSeqBeforeReconnect));

            var reconnectEvent = session.ReadEventsSince(0).FirstOrDefault(evt =>
                evt.Type == "PlayerJoined" &&
                evt.Payload.TryGetValue("reconnected", out var marker) &&
                marker is bool isReconnected &&
                isReconnected);
            Assert.That(reconnectEvent.Seq, Is.GreaterThan(0));
            Assert.That(reconnectEvent.Payload["resumeFromSeq"], Is.EqualTo(reconnect.resumeFromSeq));

            var hostDisconnect = session.OnDisconnect("host-1", "host-1");
            Assert.That(hostDisconnect.ok, Is.True);
            Assert.That(hostDisconnect.action, Is.EqualTo("SAFE_TERMINATE"));
        }
    }
}
