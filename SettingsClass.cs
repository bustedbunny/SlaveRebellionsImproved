using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
using UnityEngine;

namespace SlaveRebellionsImproved
{
    class SettingsClass
    {
        public class SlaveSettings : ModSettings
        {

            public float rebellionMin = 0.6f;
            public float rebellionLoyal = 0.8f;

            public override void ExposeData()
            {
                Scribe_Values.Look(ref rebellionMin, "rebellionMin", 0.6f);
                Scribe_Values.Look(ref rebellionLoyal, "rebellionLoyal", 0.8f);
                base.ExposeData();
                if (rebellionMin == 0f && rebellionLoyal == 0f)
                {
                    rebellionMin = 0.6f;
                    rebellionLoyal = 0.8f;
                }
            }
        }

        public class SlaveMod : Mod
        {
            readonly SlaveSettings settings;
            public SlaveMod(ModContentPack content) : base(content)
            {
                this.settings = GetSettings<SlaveSettings>();
            }

            public override void DoSettingsWindowContents(Rect inRect)
            {
                Listing_Standard listingStandard = new Listing_Standard();
                listingStandard.Begin(inRect);
                listingStandard.Label("MinSupressionLabel".Translate());
                listingStandard.Label("CurrentValueForSlaves".Translate(Math.Round(settings.rebellionMin * 100, 2)));
                settings.rebellionMin = listingStandard.Slider(settings.rebellionMin, 0f, 1f);
                
                if (settings.rebellionLoyal < settings.rebellionMin)
                {
                    settings.rebellionLoyal = settings.rebellionMin;
                }
                listingStandard.Label("LoyalSupressionLabel".Translate());
                listingStandard.Label("CurrentValueForSlaves".Translate(Math.Round(settings.rebellionLoyal*100,2)));
                settings.rebellionLoyal = listingStandard.Slider(settings.rebellionLoyal, settings.rebellionMin, 1f);
                
                if (listingStandard.ButtonText("SlaveDefaultValues".Translate()))
                {
                    settings.rebellionMin = 0.6f;
                    settings.rebellionLoyal = 0.8f;
                }
                listingStandard.End();
                base.DoSettingsWindowContents(inRect);
            }

            public override string SettingsCategory()
            {
                return "Slave Rebellions Improved";
            }
        }
    }
}
