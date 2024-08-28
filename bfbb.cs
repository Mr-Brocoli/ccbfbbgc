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

using System.Threading.Tasks;
using JetBrains.Annotations;
using System.Runtime.InteropServices;
using System.Globalization;



namespace CrowdControl.Games.Packs.BFBB
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
            public const uint maxHealth = baseAddr + 0x1738;

            public static uint oldHealth;

            public static uint slipTimer = baseAddr + 0x1748;
            public static uint slipAmount = baseAddr + 0x174c;

            public static uint jumpHeightSpongeBob = baseAddr + 0xA08;
            public static uint jumpHeightPatrick = baseAddr + 0xE50;
            public static uint jumpHeightSandy = baseAddr + 0x1298;

            public static uint fogStrength = baseAddr + 0x664;
            public static float initialFogStrength;

            public static uint shinyCount = baseAddr + 0x1B00;

            public static uint isInControlFlags = baseAddr + 0x1788;

        }


        private const uint TOC = 0x803d4980;

        private const uint isForceCruiseBubble = TOC - 0x3f00;

        private const uint playerSpeed = TOC - 0x3EFC;

        private const uint isBowlingBallForced = TOC - 0x3EF8;
        private const uint isStopBowlingBall = 0x803d2900 - 0x72D0;

        private const uint honestlyIDK = TOC - 0x6864;

        private const uint invertedMovement = TOC - 0x6860;

        private const uint invertedMovementReal = TOC - 0x3ef4;

        private const uint isMirrorMode = TOC - 0x3ef0;

        private const uint isUpsideDown = TOC - 0x3eec;

        private const uint isInvincibubbly = TOC - 0x3ee8;

        private const uint isTextureConstant = TOC - 0x3ee4;

        //disable for constant bubbles 800757cc

        //-0x7480 is like the movement speed of shinies maybe?

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
                    new Effect("OHKO", "ohko"){Price=25, Duration=20},
                    new Effect("Ender Bubble", "enderbubble"){Price = 10},
                    new Effect("Slippery Movement", "icefloor"){Price = 10, Duration=15},
                    new Effect("FAST PLAYER!!!", "playerspeed_200"){Price = 10, Duration=10},
                    new Effect("Slow Player", "playerspeed_50"){Price = 10, Duration=10},
                    new Effect("Super Jump", "jumppower_100"){Price = 10, Duration=10},
                    new Effect("The Fog is Coming", "fog"){Price = 10, Duration=20},
                    new Effect("Bowling Time", "bowlingball"){Price = 10, Duration=10},
                    new Effect("Use your DPAD", "useyourdpad"){Price = 10, Duration=10},
                    new Effect("Inverted Movement", "invertedmovement"){Price = 10, Duration=10},
                    new Effect("Big Player", "playerscale_200"){Price = 10, Duration=10},
                    new Effect("small player", "playerscale_50"){Price = 10, Duration=10},
                    new Effect("Mirror Mode", "mirrormode"){Price = 10, Duration=20},
                    new Effect("Upside Down", "upsidedown"){Price = 10, Duration=10},
                    new Effect("Invincibubbly", "invincibubbly"){Price = 10, Duration=10},
                    new Effect("Texture Constant Funny", "textureconstant"){Price = 10, Duration=10},
                    new Effect("Give Shinies", "giveshinies"){Quantity=99999, Price = 10},
                    new Effect("Take Shinies", "takeshinies"){Quantity=99999, Price = 10},
                    new Effect("Increase Max HP", "givemaxhp"){Price = 100},
                    new Effect("Decrease Max HP", "takemaxhp"){Price=100},
                };
                return effects;
            }
        }

        public override ROMTable ROMTable => new[]
{
        new ROMInfo("BFBB (NTSC-U) (GMSE01)", null, Patching.Ignore, ROMStatus.ValidPatched,
            s => Patching.MD5(s, "0c6d2edae9fdf40dfc410ff1623e4119"))
        };



        public override Game Game { get; } = new("BFBB", "BFBB", "GCN", ConnectorType.GCNConnector);

        //protected override bool IsReady(EffectRequest request)
        //{
        //    return true;
        //}

        protected override GameState GetGameState()
        {

            if(!Connector.Read32(0x803d2900 - 0x7058, out uint basicPauseCheck))
                return GameState.Unknown;

            if (basicPauseCheck >= 6 && basicPauseCheck <= 8)
                return GameState.Paused;

            if (!Connector.Read32(Player.isInControlFlags, out uint isInControl))
                return GameState.Unknown;

            if (isInControl != 0)
                return GameState.Paused;

            return GameState.Ready;
        }


        private bool geckoFlush(uint[] g)
        {
            sendGeckoTo = sendGeckoToBase;
            for (uint i = 0; i != g.Length; i += 2)
            {
                uint addr = g[i] & 0xffffff | 0x80000000;
                if (g[i] >> 24 == 0x04)
                {
                    if (!Connector.Write32(addr, g[i + 1])) 
                        return false;
                }
                else if (g[i] >> 24 == 0xc2)
                {
                    uint j = (g[i + 1] * 2) + i + 2;
                    i += 2;
                    long k = (sendGeckoTo - (long)(addr));
                    if(!Connector.Write32(addr, (uint)(0x48000000 | (k & 0x3ffffff))))
                        return false;
                    while (i != j)
                    {
                        if(!Connector.Write32(sendGeckoTo, g[i]))
                            return false;
                        i++;
                        sendGeckoTo += 4;
                    }

                    sendGeckoTo -= 4;
                    k = (addr + 4 - (long)(sendGeckoTo));
                    if (!Connector.Write32(sendGeckoTo, (uint)(0x48000000 | (k & 0x3ffffff))))
                        return false ;
                    sendGeckoTo += 4;
                    i -= 2;
                }
            }
            return true;
            //(Connector as DolphinConnector)?.UncacheJIT();
        }

        //Try Effect Simple
        private void TryEffect(EffectRequest request, Func<bool> action) =>
            TryEffect(request, () => true, action);

        protected override void StartEffect(EffectRequest request)
        {

            //init();

            //if (!IsReady(request))
            //{
            //    DelayEffect(request, TimeSpan.FromSeconds(5));
            //    return;
            //}

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
                case "takeshinies":
                case "giveshinies":
                    TryEffect(request,
                       () =>
                       {
                           return Connector.RangeAdd32(Player.shinyCount, (codeParams[0] == "takeshinies" ? -1 : 1) * request.Quantity, 0, 99999, false);
                       });
                    break;
                case "takemaxhp":
                case "givemaxhp":
                    TryEffect(request,
                       () =>
                       {
                           return Connector.RangeAdd32(Player.maxHealth, (codeParams[0] == "takemaxhp" ? -1 : 1), 0, 6, false)
                           && Connector.Read32(Player.maxHealth, out uint maxHealth) && Connector.Write32(Player.health, maxHealth);
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

                           return Connector.Read32(Player.maxHealth, out uint maxHealth) && maxHealth > 1 &&
                                  Connector.Read32(Player.health, out Player.oldHealth) && Player.oldHealth > 1 && Connector.Write32(Player.health, 1);
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
                      RepeatAction(request,
                            () => true,
                            () => Connector.SendMessage($"{request.DisplayViewer} has casted the fog upon you."), TimeSpan.FromSeconds(1),
                            () => true, TimeSpan.FromMilliseconds(100),
                            () =>
                            {
                                return Connector.ReadFloat(Player.fogStrength, out float x) && Connector.WriteFloat(Player.fogStrength, x > 20.0f ? x - Player.initialFogStrength / 150.0f : 10.0f);

                            }, TimeSpan.FromMilliseconds(100), false, "fog");
                    break;
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
                case "playerscale":
                    StartTimed(request,
                       () => true,
                       () =>
                       {
                           float scale = float.Parse(codeParams[1]) / 100.0f;

                           if (!Connector.Read32(0x803c0c38 + 0x24, out uint model))
                               return false;

                           do
                           {
                               if (!Connector.WriteFloat(model + 0x50, scale) || !Connector.WriteFloat(model + 0x54, scale) || !Connector.WriteFloat(model + 0x58, scale))
                                   return false;
                           } while (Connector.Read32(model, out model) && model!=0);
                           return true;

                       }, "playerscale");
                    break;
                case "mirrormode":
                    StartTimed(request,
                       () => true,
                       () =>
                       {
                           return Connector.Write32(isMirrorMode, 1);

                       }, "mirrormode");
                    break;
                case "upsidedown":
                    StartTimed(request,
                       () => true,
                       () =>
                       {
                           return Connector.Write32(isUpsideDown, 1);

                       }, "upsidedown");
                    break;
                case "invincibubbly":
                    StartTimed(request,
                       () => true,
                       () =>
                       {
                           return Connector.Write32(isInvincibubbly, 1);

                       }, "invincibubbly");
                    break;
                case "textureconstant":
                    StartTimed(request,
                       () => true,
                       () =>
                       {
                           return Connector.Write32(isTextureConstant, 1);

                       }, "textureconstant");
                    break;

            }


            return;


        }


        protected override bool StopEffect(EffectRequest request)
        {
            string[] codeParams = request.EffectID.Split('_');
            switch (codeParams[0])
            {
                case "ohko":
                    
                    return Connector.Read32(Player.health, out uint healthCheck) && healthCheck > Player.oldHealth ? true : Connector.Write32(Player.health, Player.oldHealth);
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
                case "playerscale":
                    if (!Connector.Read32(0x803c0c38 + 0x24, out uint model))
                        return false;

                    do
                    {
                        if (!Connector.WriteFloat(model + 0x50, 1.0f) || !Connector.WriteFloat(model + 0x54, 1.0f) || !Connector.WriteFloat(model + 0x58, 1.0f))
                            return false;
                    } while (Connector.Read32(model, out model) && model!=0);
                    return true;
                case "mirrormode":
                    return Connector.Write32(isMirrorMode, 0);
                case "upsidedown":
                    return Connector.Write32(isUpsideDown, 0);
                case "invincibubbly":
                    return Connector.Write32(isInvincibubbly, 0);
                case "textureconstant":
                    return Connector.Write32(isTextureConstant, 0);
                default:
                    return base.StopEffect(request);

            }
        }

        public override bool StopAllEffects()
        {
            bool result = base.StopAllEffects();
            return result;
        }
    }
}
