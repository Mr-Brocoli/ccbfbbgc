using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.InteropServices;
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

        }

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
                    new Effect("OHKO", "ohko"){Price=25, Duration=10},

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
                case "notbeohko":
                    TryEffect(request,
                       () =>
                       {
                           return true;
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
               

            }
        }

        protected override bool StopEffect(EffectRequest request)
        {
            string[] codeParams = request.EffectID.Split('_');
            switch (codeParams[0])
            {
                case "ohko":
                    return true;
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
