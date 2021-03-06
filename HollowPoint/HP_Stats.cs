﻿using System;
using System.Collections;
using UnityEngine;
using static UnityEngine.Random;
using static Modding.Logger;
using Modding;
using ModCommon.Util;
using MonoMod;
using Language;
using System.Xml;


namespace HollowPoint
{
    class HP_Stats : MonoBehaviour
    {
        public static event Action<int> ShardAmountChanged;

        public static int fireSoulCost = 1;
        public static int burstSoulCost = 15;

        const int DEFAULT_SINGLE_COST = 3;
        const int DEFAULT_BURST_COST = 1;

        const float DEFAULT_ATTACK_SPEED = 0.41f;
        const float DEFAULT_ATTACK_SPEED_CH = 0.25f;

        const float DEFAULT_ANIMATION_SPEED = 0.35f;
        const float DEFAULT_ANIMATION_SPEED_CH = 0.28f;
        float soulRegenTimer = 3f;
        float max_soul_regen = 33;
        float passiveSoulTimer = 3f;

        public static float walkSpeed = 3f;
        public static float fireRateCooldown = 5.75f;
        public static float fireRateCooldownTimer = 5.75f;

        public static float bulletRange = 0;
        public static float heatPerShot = 0;
        public static float bulletVelocity = 0;

        public static bool canFire = false;
        static float recentlyFiredTimer = 60f;

        int soulConsumed = 0;
        public static int soulGained = 0;

        public static bool hasActivatedAdrenaline = false;

        int totalGeo = 0;

        //Dash float values
        float default_dash_cooldown = 0;
        float default_dash_cooldown_charm;
        float default_dash_speed = 0;
        float default_dash_speed_sharp = 0;
        float default_dash_time = 0;
        float default_gravity = 0;

        public static int currentPrimaryAmmo;
        public static int grenadeAmnt = 0;

        public static string soundName = "";
        public static string bulletSprite = "";

        public PlayerData pd_instance;
        public HeroController hc_instance;
        public AudioManager am_instance;

        public void Awake()
        {
            StartCoroutine(InitStats());
        }

        public IEnumerator InitStats()
        {
            while (HeroController.instance == null)
            {
                yield return null;
            }

            pd_instance = PlayerData.instance;
            hc_instance = HeroController.instance;
            am_instance = GameManager.instance.AudioManager;


            Log("Default Dash Cooldown " + hc_instance.DASH_COOLDOWN);
            Log("Default Dash Cooldown Charm " + hc_instance.DASH_COOLDOWN_CH);
            Log("Default Dash Speed " + hc_instance.DASH_SPEED);
            Log("Default Dash Speed Sharp " + hc_instance.DASH_SPEED_SHARP);
            Log("Default Dash Time " + hc_instance.DASH_TIME);
            Log("Default Dash Gravity " + hc_instance.DEFAULT_GRAVITY);
            //Log(am_instance.GetAttr<float>("Volume"));

            //On.BuildEquippedCharms.BuildCharmList += BuildCharm;

            ModHooks.Instance.CharmUpdateHook += CharmUpdate;
            ModHooks.Instance.FocusCostHook += FocusCost;
            ModHooks.Instance.LanguageGetHook += LanguageHook;
            ModHooks.Instance.SoulGainHook += Instance_SoulGainHook;
            ModHooks.Instance.BlueHealthHook += Instance_BlueHealthHook;
            On.HeroController.CanNailCharge += HeroController_CanNailCharge;
            //On.HeroController.AddGeo += HeroController_AddGeo;
        }

        private int Instance_SoulGainHook(int num)
        {
            return 8;
        }

        private void HeroController_AddGeo(On.HeroController.orig_AddGeo orig, HeroController self, int amount)
        {
            totalGeo += amount;

            if(totalGeo >= 50)
            {
                HeroController.instance.AddMPChargeSpa(15);
                totalGeo = 0;
            }

            orig(self, amount);
        }

        private int Instance_BlueHealthHook()
        {

            return 0;
        }

        private bool HeroController_CanNailCharge(On.HeroController.orig_CanNailCharge orig, HeroController self)
        {
            if (HP_WeaponHandler.currentGun.gunName == "Nail")
                return orig(self);

            return false;
        }

        public string LanguageHook(string key, string sheet)
        {
            string txt = Language.Language.GetInternal(key, sheet);
            //Modding.Logger.Log("KEY: " + key + " displays this text: " + txt);

            string nodePath = "/TextChanges/Text[@name=\'" + key + "\']";
            XmlNode newText = LoadAssets.textChanges.SelectSingleNode(nodePath);


            if (newText == null)
            {
                return txt;
            }
            //Modding.Logger.Log("NEW TEXT IS " + newText.InnerText);

            string replace = newText.InnerText.Replace("$", "<br><br>");
            replace = replace.Replace("#", "<page>");
            return replace;
        }

        private float FocusCost()
        {
            //return (float)PlayerData.instance.GetInt("MPCharge") / 35.0f;
            recentlyFiredTimer = 60;

            if (PlayerData.instance.equippedCharm_27)
            {
                soulConsumed += 1;

                if (soulConsumed % 66 == 0)
                {
                    if (grenadeAmnt < 10)
                        grenadeAmnt += 1;
                }
            }

            if (hasActivatedAdrenaline && PlayerData.instance.equippedCharm_23) return 3f;

            return 1f;
        }

        public void CharmUpdate(PlayerData data, HeroController controller)
        {
            Log("Charm Update Called");
            HP_AttackHandler.airStrikeActive = false;
            bulletSprite = "";

            //Default Dash speeds
            default_dash_cooldown = 0.6f;
            default_dash_cooldown_charm = 0.4f;
            default_dash_speed = 20f;
            default_dash_speed_sharp = 28f;
            default_dash_time = 0.25f;
            default_gravity = 0.79f;

            //Initialise stats
            currentPrimaryAmmo = 10;
            bulletRange = .20f + (PlayerData.instance.nailSmithUpgrades * 0.02f);
            bulletVelocity = 44f;
            burstSoulCost = 1;
            fireRateCooldown = 12.5f;
            fireSoulCost = 5;
            grenadeAmnt = 2 + (int)(Math.Floor((float)(PlayerData.instance.nailSmithUpgrades + 1) / 2));
            heatPerShot = 1f;
            max_soul_regen = 25;
            soulGained = 2;
            soulConsumed = 0;
            soulRegenTimer = 2.75f;
            walkSpeed = 3.5f;

            //Charm 3 Grubsong
            soulRegenTimer = (PlayerData.instance.equippedCharm_3) ? 1.25f : 2.75f;

            //Charm 6 Fury of the Fallen
            if (PlayerData.instance.equippedCharm_6)
            {
                walkSpeed += 2f;
                soulRegenTimer -= 1f;
                fireRateCooldown -= 0.4f;
            }

            //Charm 8 Lifeblood Heart
            currentPrimaryAmmo += (PlayerData.instance.equippedCharm_8)? 2 : 0;

            //Charm 11 Flukenest, add additional soul cost
            if (PlayerData.instance.equippedCharm_11)
            {
                heatPerShot += 3f;
                fireSoulCost += 7;
                fireRateCooldown += 6.5f;
                bulletRange += -0.025f;
            }

            //Charm 13 Mark of Pride, increase range, increases heat, increases soul cost, decrease firing speed (SNIPER MODULE)
            if (PlayerData.instance.equippedCharm_13)
            {
                bulletRange += 0.5f;
                bulletVelocity += 15f;
                heatPerShot -= 0.55f;
                fireSoulCost += 5;
                fireRateCooldown += 3.75f;
                walkSpeed += -1f;
            }
            //yeet

            //Charm 14 Steady Body 
            //walkSpeed = (PlayerData.instance.equippedCharm_14) ? (walkSpeed) : walkSpeed;

            //Charm 16 Sharp Shadow and Fury of the fallen sprite changes
            bulletSprite = (PlayerData.instance.equippedCharm_16) ? "shadebullet.png" : bulletSprite;
            bulletSprite = (PlayerData.instance.equippedCharm_6) ? "furybullet.png" : bulletSprite;

            //Charm 18 Long Nail
            bulletVelocity += (PlayerData.instance.equippedCharm_18) ? 20f : 0;

            //Charm 19 Shaman Stone
            grenadeAmnt += (PlayerData.instance.equippedCharm_19) ? (PlayerData.instance.nailSmithUpgrades + 2) : 0;

            soulGained += (PlayerData.instance.equippedCharm_20) ? 1 : 0;

            //Charm 21 Soul Eater
            if (PlayerData.instance.equippedCharm_21)
            {
                fireSoulCost -= 2;
                max_soul_regen += 10;
                soulGained += 2;
            }

            //Charm 23 Fragile/Unbrekable Heart
            hasActivatedAdrenaline = (PlayerData.instance.equippedCharm_23) ? false : true;

            //Charm 25 Fragile Strength
            heatPerShot += (PlayerData.instance.equippedCharm_25) ? 0.25f : 0;

            //Charm 32 Quick Slash, increase firerate, decrease heat, 
            if (PlayerData.instance.equippedCharm_32)
            {
                heatPerShot += 0.3f;
                fireSoulCost += 1;
                fireRateCooldown -= 2.5f;
                walkSpeed += -2.25f;
            }

            //Charm 37 Sprint
            walkSpeed += 1.75f;

            //Charm 37 Sprintmaster 

            //Minimum value setters, NOTE: soul cost doesnt like having it at 1 so i set it up as 2 minimum
            fireSoulCost = (fireSoulCost < 2) ? 2 : fireSoulCost;
            walkSpeed = (walkSpeed < 1) ? 1 : walkSpeed;
            fireRateCooldown = (fireRateCooldown < 1f)? 1f: fireRateCooldown;

            ShardAmountChanged?.Invoke(currentPrimaryAmmo);

            HP_UIHandler.UpdateDisplay();
        }



        void Update()
        {
            if (fireRateCooldownTimer >= 0)
            {
                fireRateCooldownTimer -= Time.deltaTime * 30f;
                //canFire = false;
            }
            else
            {
                canFire = true;
            }


            if (HP_SpellControl.buffActive && PlayerData.instance.equippedCharm_35)
            {
                HeroController.instance.SetAttr<bool>("doubleJumped", false);
            }

            if (HP_SpellControl.buffActive && PlayerData.instance.equippedCharm_4)
            {
                //HeroController.instance.cState.invulnerable = true;
            }

            //actually put this on the weapon handler so its not called 24/7
            if (HP_WeaponHandler.currentGun.gunName != "Nail") // && !HP_HeatHandler.overheat
            {
                hc_instance.ATTACK_DURATION = 0.0f;
                hc_instance.ATTACK_DURATION_CH = 0f;

                hc_instance.ATTACK_COOLDOWN_TIME = 500f;
                hc_instance.ATTACK_COOLDOWN_TIME_CH = 500f;
            }
            else
            {
                hc_instance.ATTACK_COOLDOWN_TIME = DEFAULT_ANIMATION_SPEED;
                hc_instance.ATTACK_COOLDOWN_TIME_CH = DEFAULT_ANIMATION_SPEED_CH;

                hc_instance.ATTACK_DURATION = DEFAULT_ATTACK_SPEED;
                hc_instance.ATTACK_DURATION_CH = DEFAULT_ATTACK_SPEED_CH;
            }       
        }


        void FixedUpdate()
        {
            if (hc_instance.cState.isPaused) return;

            //Soul Gain Timer
            if (recentlyFiredTimer >= 0)
            {
                recentlyFiredTimer -= Time.deltaTime * 30f;
            }
            else if (passiveSoulTimer > 0)
            {
                passiveSoulTimer -= Time.deltaTime * 30f;
            }
            else if(currentPrimaryAmmo < 15) //pd_instance.MPCharge < max_soul_regen
            {
                passiveSoulTimer = soulRegenTimer;
                //IncreaseArtifactPower();
                //HeroController.instance.AddMPCharge(1);
            }
        }

        public static int CalculateDamage(Vector3 bulletOriginPosition, Vector3 enemyPosition)
        {
            int dam = 6;
            float distance = Vector3.Distance(bulletOriginPosition, enemyPosition);


            //dam = (int)((distance <= 2 || distance >= 8) ? dam*0.5f : ((distance > 2 && distance <= 4) || (distance > 5 && distance <= 7)) ? dam * 1f : dam * 2f); 
            //dam = (int)((distance >= 8) ? dam * 0.5f : ((distance >= 0 && distance <= 4) || (distance > 5 && distance <= 7)) ? dam * 1f : dam * 2f);
            dam = (int)((distance >= 8)? dam * 0.75f : ((distance >= 4)? dam * 1 : dam * 1.25f));


            Log("dealt " + dam);

            return dam;
            int damage = Range(2, 5) + PlayerData.instance.nailSmithUpgrades * 3; 
            //Flukenest
            if (PlayerData.instance.equippedCharm_11)
            {
                damage = (int)(damage * .5f);
            }
            //Mark of Pride
            if (PlayerData.instance.equippedCharm_13)
            {
                float travelDistance = Vector3.Distance(bulletOriginPosition, enemyPosition);
                Modding.Logger.Log("Travel distance " + travelDistance);
                damage = (int)(damage * 2.5f);
            }
            //Fury of the Fallen
            if (PlayerData.instance.equippedCharm_6)
            {
                damage = (int)(damage * 1.2f);
            }

            return damage;
        }

        public static int CalculateSoulGain()
        {
            int soul = 3;//soulGained;
            return soul;
        }

        public static void ReduceGrenades()
        {
            if (PlayerData.instance.equippedCharm_33)
            {
                int chance = UnityEngine.Random.Range(0, 3);
                Modding.Logger.Log("chance " + chance);
                if (chance != 0)
                {
                    grenadeAmnt -= 0;
                    return;
                }
            }
            grenadeAmnt -= 1;
        }

        public static void ReloadGun(int reloadAmount)
        {
            currentPrimaryAmmo = reloadAmount;
            ShardAmountChanged?.Invoke(1 * currentPrimaryAmmo);
        }

        public static void IncreaseArtifactPower(int increaseAmount)
        {
            currentPrimaryAmmo += increaseAmount;
            ShardAmountChanged?.Invoke(1 * currentPrimaryAmmo);
        }

        public static void ReduceAmmunition()
        {
            currentPrimaryAmmo -= 1;
            ShardAmountChanged?.Invoke(-1 * currentPrimaryAmmo);
        }

        public static void DisplayAmmoCount()
        {

        }

        //Utility Methods
        public static void StartBothCooldown()
        {
            //Log("Starting cooldown");
            fireRateCooldownTimer = -1;
            fireRateCooldownTimer = fireRateCooldown;
            //fireRateCooldownTimer = 0.1f;
            canFire = false;
            recentlyFiredTimer = 60;
        }

        void OnDestroy()
        {
            ModHooks.Instance.CharmUpdateHook -= CharmUpdate;
            ModHooks.Instance.FocusCostHook -= FocusCost;
            ModHooks.Instance.LanguageGetHook -= LanguageHook;
            ModHooks.Instance.SoulGainHook -= Instance_SoulGainHook;
            ModHooks.Instance.BlueHealthHook -= Instance_BlueHealthHook;
            On.HeroController.CanNailCharge -= HeroController_CanNailCharge;
            Destroy(gameObject.GetComponent<HP_Stats>());
        }
        
    }
}
