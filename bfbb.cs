using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

//using System.Runtime.Remoting.Metadata.W3cXsd2001;
using CrowdControl.Common;
//using CrowdControl.Games.SpecialConnectors;
using JetBrains.Annotations;
using ConnectorType = CrowdControl.Common.ConnectorType;

namespace CrowdControl.Games.Packs
{
    [UsedImplicitly]
    [SuppressMessage("ReSharper", "CommentTypo")]
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    [SuppressMessage("ReSharper", "StringLiteralTypo")]
    public class BFBB : GCNEffectPack
    {
        public BFBB(UserRecord player, Func<CrowdControlBlock, bool> responseHandler,
            Action<object> statusUpdateHandler) : base(player, responseHandler, statusUpdateHandler)
        {
            init();
        }


        public static class Player
        {
            public const uint baseAddr = 0x803c0558;

            public const uint health = baseAddr + 0x16b0;

            public static uint oldHealth;

            public static uint slipTimer = baseAddr + 0x1748;
            public static uint slipAmount = baseAddr + 0x174c;

            public static uint jumpHeightSpongeBob = baseAddr + 0xA08;
            public static uint jumpHeightPatrick = baseAddr + 0xE50;
            public static uint jumpHeightSandy = baseAddr + 0x1298;

            public static uint fogStrength = baseAddr + 0x664;
            public static float initialFogStrength;

        }


        private const uint TOC = 0x803d4980;

        private const uint isForceCruiseBubble = TOC - 0x3f00;

        private const uint playerSpeed = TOC - 0x3EFC;

        private const uint isBowlingBallForced = TOC - 0x3EF8;
        private const uint isStopBowlingBall = 0x803d2900 - 0x72D0;

        private const uint honestlyIDK = TOC - 0x6864;

        private const uint invertedMovement = TOC - 0x6860;

        private const uint invertedMovementReal = TOC - 0x3ef4;

        private const uint sendGeckoToBase = 0;
        private static uint sendGeckoTo = sendGeckoToBase;
        private uint[] generalcode = { 0 };



        private void init()
        {

        }


        public override EffectList Effects
        {
            get
            {
                List<Effect> effects = new List<Effect>
                {
                     new Effect("Die", "die"){Price=50},
                    new Effect("OHKO", "ohko"){Price=25, Duration=10},
                    new Effect("Ender Bubble", "enderbubble"){Price = 10},
                    new Effect("Slippery Movement", "icefloor"){Price = 10, Duration=15},
                    new Effect("FAST PLAYER!!!", "playerspeed_200"){Price = 10, Duration=10},
                    new Effect("Slow Player", "playerspeed_50"){Price = 10, Duration=10},
                    new Effect("Super Jump", "jumppower_100"){Price = 10, Duration=10},
                    new Effect("The Fog is Coming", "fog"){Price = 10, Duration=20},
                    new Effect("Bowling Time", "bowlingball"){Price = 10, Duration=10},
                    new Effect("Use your DPAD", "useyourdpad"){Price = 10, Duration=10},
                    new Effect("Inverted Movement", "invertedmovement"){Price = 10, Duration=10},
                };
                return effects;
            }
        }

        public override Game Game { get; } = new(165, "BFBB", "BFBB", "GCN",
            ConnectorType.GCNConnector);

        protected override bool IsReady(EffectRequest request)
        {
            return true;
        }

        private uint getAddressInner(params uint[] args)
        {
            uint addr = 0;
            foreach (uint t in args)
            {
                Connector.Read32(addr + t, out addr);
                if (addr == 0) return 0;
            }
            return addr;
        }

        private void geckoFlush(uint[] g)
        {
            sendGeckoTo = sendGeckoToBase;
            for (uint i = 0; i != g.Length; i += 2)
            {
                uint addr = g[i] & 0xffffff | 0x80000000;
                if (g[i] >> 24 == 0x04)
                {
                    Connector.Write32(addr, g[i + 1]);
                }
                else if (g[i] >> 24 == 0xc2)
                {
                    uint j = (g[i + 1] * 2) + i + 2;
                    i += 2;
                    long k = (sendGeckoTo - (long)(addr));
                    Connector.Write32(addr, (uint)(0x48000000 | (k & 0x3ffffff)));
                    while (i != j)
                    {
                        Connector.Write32(sendGeckoTo, g[i]);
                        i++;
                        sendGeckoTo += 4;
                    }

                    sendGeckoTo -= 4;
                    k = (addr + 4 - (long)(sendGeckoTo));
                    Connector.Write32(sendGeckoTo, (uint)(0x48000000 | (k & 0x3ffffff)));
                    sendGeckoTo += 4;
                    i -= 2;
                }
            }

            //(Connector as DolphinConnector)?.UncacheJIT();
        }

        //Try Effect Simple
        private void TryEffect(EffectRequest request, Func<bool> action) =>
            TryEffect(request, () => true, action);

        protected override void StartEffect(EffectRequest request)
        {

            init();

            if (!IsReady(request))
            {
                DelayEffect(request, TimeSpan.FromSeconds(5));
                return;
            }

            Connector.SendMessage(request.EffectID);
            string[] codeParams = request.EffectID.Split('_');
            switch (codeParams[0])
            {
                case "enderbubble":
                    TryEffect(request,
                       () =>
                       {
                           return Connector.Read32(isForceCruiseBubble, out uint isForceCruiseBubbleReal) && isForceCruiseBubbleReal==0 && Connector.Write32(isForceCruiseBubble, 1);
                       });
                    break;
                case "die":
                    TryEffect(request,
                       () =>
                       {
                           return Connector.Write32(Player.health, 0);
                       });
                    break;
                case "ohko":
                    StartTimed(request,
                       () => true,
                       () =>
                       {

                           return Connector.Read32(Player.health, out Player.oldHealth) && Connector.Write32(Player.health, 1);
                       }, "ohko");
                    break;
                case "icefloor":
                    StartTimed(request,
                       () => true,
                       () =>
                       {
                           return Connector.WriteFloat(Player.slipTimer, 9999.0f) && Connector.WriteFloat(Player.slipAmount, 1000.0f);
                       }, "icefloor");
                    break;
                case "playerspeed":
                    StartTimed(request,
                       () => true,
                       () =>
                       {
                           return Connector.WriteFloat(playerSpeed, float.Parse(codeParams[1]) / 100.0f);
                       }, "playerspeed");
                    break;
                case "jumppower":
                    StartTimed(request,
                       () => true,
                       () =>
                       {
                           //Negative because it's actually a downwards velocity added to your initial jump height, so we want to add negative falling velocity
                           float h = -float.Parse(codeParams[1]);
                           return Connector.WriteFloat(Player.jumpHeightSpongeBob, h) && Connector.WriteFloat(Player.jumpHeightPatrick, h) && Connector.WriteFloat(Player.jumpHeightSandy, h);
                       }, "jumpingorbowling");
                    break;
                case "fog":
                    RepeatAction(request, TimeSpan.FromSeconds(20),
                       () => Connector.ReadFloat(Player.fogStrength, out Player.initialFogStrength),
                       () => true,
                       TimeSpan.FromSeconds(0.1),
                       () => true,
                       TimeSpan.FromSeconds(0.1),
                       () =>
                       {
                           return Connector.ReadFloat(Player.fogStrength, out float x) && Connector.WriteFloat(Player.fogStrength, x > 20.0f ? x - Player.initialFogStrength / 150.0f : 10.0f);

                       }, TimeSpan.FromSeconds(0.1), true, "fog");
                    return;
                case "bowlingball":
                    StartTimed(request,
                       () => true,
                       () =>
                       {
                           return Connector.Write32(isBowlingBallForced, 1);
                       }, "jumpingorbowling");
                    break;
                case "useyourdpad":
                    StartTimed(request,
                       () => true,
                       () =>
                       {
                           return Connector.WriteFloat(honestlyIDK, 30.0f);
                       }, "useyourdpad");
                    break;
                case "invertedmovement":
                    StartTimed(request,
                       () => true,
                       () =>
                       {
                           return Connector.WriteFloat(invertedMovementReal, 1);
                       }, "invertedmovement");
                    break;


            }
        }

        protected override bool StopEffect(EffectRequest request)
        {
            string[] codeParams = request.EffectID.Split('_');
            switch (codeParams[0])
            {
                case "ohko":
                    return Connector.Write32(Player.health, Player.oldHealth);
                case "icefloor":
                    return Connector.WriteFloat(Player.slipTimer, 0.0f);
                case "playerspeed":
                    return Connector.WriteFloat(playerSpeed, 1.0f);
                case "jumppower":
                    return Connector.WriteFloat(Player.jumpHeightSpongeBob, 5.0f) && Connector.WriteFloat(Player.jumpHeightPatrick, 5.0f) && Connector.WriteFloat(Player.jumpHeightSandy, 5.0f);
                case "fog":
                    return Connector.WriteFloat(Player.fogStrength, Player.initialFogStrength);
                case "bowlingball":
                    return Connector.Write32(isBowlingBallForced, 0) && Connector.Write32(isStopBowlingBall, 1);
                case "useyourdpad":
                    return Connector.WriteFloat(honestlyIDK, -40.0f);
                case "invertedmovement":
                    return Connector.WriteFloat(invertedMovementReal, 0);
                default:
                    return true;

            }
        }

        public override bool StopAllEffects()
        {
            bool result = base.StopAllEffects();
            return result;
        }
    }
}
