using System.Collections.Generic;
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
            var created = session.CreateRoom(4, 5);
            Assert.That(created.ok, Is.True);
            Assert.That(session.RoomId, Is.EqualTo("room-0001"));
            Assert.That(session.InviteCode, Is.EqualTo("WLK0001"));

            var joined = session.JoinPlayer("p2");
            Assert.That(joined.ok, Is.True);

            var synced = session.SyncEvent("RoundStarted", new Dictionary<string, object> { { "round", 1 } });
            Assert.That(synced.ok, Is.True);
            Assert.That(synced.@event!.Value.Seq, Is.GreaterThan(0));

            var feed = session.ReadEventsSince(0);
            Assert.That(feed.Count, Is.EqualTo(3));
            Assert.That(feed[0].Type, Is.EqualTo("RoomCreated"));
            Assert.That(feed[1].Type, Is.EqualTo("PlayerJoined"));
            Assert.That(feed[2].Type, Is.EqualTo("RoundStarted"));

            var playerDisconnect = session.OnDisconnect("p2");
            Assert.That(playerDisconnect.ok, Is.True);
            Assert.That(playerDisconnect.action, Is.EqualTo("MARK_INACTIVE"));

            var reconnect = session.OnReconnect("p2");
            Assert.That(reconnect.ok, Is.True);
            Assert.That(reconnect.action, Is.EqualTo("RESUME_FROM_LAST_SEQUENCE"));
            Assert.That(reconnect.resumeFromSeq, Is.GreaterThan(0));

            var hostDisconnect = session.OnDisconnect("host-1");
            Assert.That(hostDisconnect.ok, Is.True);
            Assert.That(hostDisconnect.action, Is.EqualTo("SAFE_TERMINATE"));
        }
    }
}
