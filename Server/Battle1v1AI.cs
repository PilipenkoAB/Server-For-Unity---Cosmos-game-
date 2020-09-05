using System;
using System.Collections.Generic;
using System.Text;

namespace Server
{
    /*
         Battle1v1AI - class for battle, contain all information about the battle
           and all actions that can be in that battle---------------
       */
    public class Battle1v1AI
    {

        // Constructor that takes - starting point no arguments:
        public Battle1v1AI()
        {
            toStart = 0;
            started = 0;
            finished = 0;
            battleTime = 0;
            playerReady = 0;
            // Calculatet starting parameters after getting them at creating the class






        }

        public void SetStartHealth()
        {
            playerShipCurrentHealth = playerShipMaxHealth;
            aIShipCurrentHealth = aIShipMaxHealth;
        }

        public void SetStartReload()
        {
            playerWeaponSlotCurrentReloadTime[0] = playerWeaponSlotReloadTime[0];
            aIWeaponSlotCurrentReloadTime[0] = aIWeaponSlotReloadTime[0];
        }


        public void ReloadAllWeaponsPerTick()
        {
            int reloadOneTick = 50; // ms

            // reload of the player weapon
            if (playerWeaponSlotCurrentReloadTime[0] > 0)
            {
                playerWeaponSlotCurrentReloadTime[0] -= reloadOneTick;

            }
            else if (playerWeaponSlotCurrentReloadTime[0] <= 0)
            {
                playerWeaponSlotCurrentReloadTime[0] = 0;
            }


            // reload of the ai weapon
            if (aIWeaponSlotCurrentReloadTime[0] > 0)
            {
                aIWeaponSlotCurrentReloadTime[0] -= reloadOneTick;
            }
            else if (aIWeaponSlotCurrentReloadTime[0] <= 0)
            {
                aIWeaponSlotCurrentReloadTime[0] = 0;
            }
        }

        public void AttackDummyClass()
        {
            //ai attack player
            if (aIWeaponSlotCurrentReloadTime[0] == 0)
            {
                playerShipCurrentHealth -= aIWeaponSlotDamage[0];
                aIWeaponSlotCurrentReloadTime[0] = aIWeaponSlotReloadTime[0];
            }
        }

        public void PlayerAttackWeapon()
        {
            // player attack AI
            if (playerWeaponSlotCurrentReloadTime[0] == 0)
            {
                aIShipCurrentHealth -= playerWeaponSlotDamage[0];
                playerWeaponSlotCurrentReloadTime[0] = playerWeaponSlotReloadTime[0];
            }
        }

        //-----------------------------------------------------------------
        // NEW ------------------------------------------------------------
        //-----------------------------------------------------------------
        public void PlayerAttackModule(int weaponIdint, int moduleSlotId) 
        {
            // player attack AI
            if (playerWeaponSlotCurrentReloadTime[0] == 0)
            {
                aIShipCurrentHealth -= playerWeaponSlotDamage[0];
                playerWeaponSlotCurrentReloadTime[0] = playerWeaponSlotReloadTime[0];
            }
        }

        public void PlayerModuleEnergyUp(int moduleSlotId)
        {
            if (playerSlotExist[moduleSlotId] != 0 && playerSlotExist[moduleSlotId] != -1 && playerSlotPowered[moduleSlotId] == 0 && playerShipFreeEnergy <= playerShipMaxEnergy)
            {
                playerSlotPowered[moduleSlotId] = 1;
                playerShipFreeEnergy -= 1;
            }
        }

        public void PlayerModuleEnergyDown(int moduleSlotId)
        {
            if (playerSlotExist[moduleSlotId] != 0 && playerSlotExist[moduleSlotId] != -1 && playerSlotPowered[moduleSlotId] != 0 && playerShipFreeEnergy > 0)
            {
                playerSlotPowered[moduleSlotId] = 0;
                playerShipFreeEnergy += 1;
            }
        }

        //Variables
        public int battle1v1AIId { get; set; }

        public int toStart { get; set; }
        public int started { get; set; }

        public int playerReady { get; set; }
        public int finished { get; set; }
        public int battleTime { get; set; } // in ms

        // PLAYER and AI ID
        public int aiId { get; set; } // DO I NEED THIS???

        public int playerID { get; set; } // DO I NEED THIS???

        // player shipId and ai shipId
        public int playerShipId { get; set; }

        public int aIShipId { get; set; }


        ////-------------------- Player -------------------------

        // NEW SYSTEM

        // PLAYER

        // ship

        public int playerShipMaxHealth { get; set; }
        public int playerShipCurrentHealth { get; set; }
        public int playerShipMaxEnergy { get; set; }
        public int playerShipFreeEnergy { get; set; }

        // p modules

        //[ engine , cockpit, biglot1 .. 5, mediumslot 1 .. 5, smallslot 1 .. 5]

        public int[] playerSlotExist { get; set; } = new int[17] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
        public int[] playerSlotHealth { get; set; } = new int[17] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
        public int[] playerSlotPowered { get; set; } = new int[17] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
        public int[] playerSlotEnergyRequired { get; set; } = new int[17] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
        public string[] playerSlotType { get; set; } = new string[17] { "0", "0", "0", "0", "0", "0", "0", "0", "0", "0", "0", "0", "0", "0", " 0", "0", "0" };

        //bigslot difference slots
        public int[] playerSlotShieldCapacity { get; set; } = new int[17];
        public int[] playerSlotShieldRechargeTime { get; set; } = new int[17];
        public int[] playerSlotShieldRechargeCurrentTime { get; set; } = new int[17];
        public int[] playerSlotShieldRechargeRate { get; set; } = new int[17];

        public int[] playerSlotWeaponControlAmountOfWeapons { get; set; } = new int[17];

        // weapons

        public int[] playerWeaponSlotExist { get; set; } = new int[5] { 0, 0, 0, 0, 0 };
        public int[] playerWeaponSlotPowered { get; set; } = new int[5] { 0, 0, 0, 0, 0 };
        public int[] playerWeaponSlotEnergyRequired { get; set; } = new int[5] { 0, 0, 0, 0, 0 };
        public int[] playerWeaponSlotDamage { get; set; } = new int[5] { 0, 0, 0, 0, 0 };
        public int[] playerWeaponSlotReloadTime { get; set; } = new int[5] { 0, 0, 0, 0, 0 };
        public int[] playerWeaponSlotCurrentReloadTime { get; set; } = new int[5] { 0, 0, 0, 0, 0 };

        // Crew

        public int[] playerCrewExist { get; set; }
        public int[] playerCrewHealth { get; set; }
        public int[] playerCrewDamage { get; set; }



        // AI

        // ship

        public int aIShipMaxHealth { get; set; }
        public int aIShipCurrentHealth { get; set; }
        public int aIShipMaxEnergy { get; set; }
        public int aIShipFreeEnergy { get; set; }

        // p modules

        //[ engine , cockpit, biglot1 .. 5, mediumslot 1 .. 5]

        public int[] aISlotExist { get; set; } = new int[17];
        public int[] aISlotHealth { get; set; } = new int[17];
        public int[] aISlotPowered { get; set; } = new int[17];
        public int[] aISlotEnergyRequired { get; set; } = new int[17];
        public string[] aISlotType { get; set; } = new string[17];

        //bigslot difference slots
        public int[] aISlotShieldCapacity { get; set; } = new int[17];
        public int[] aISlotShieldRechargeTime { get; set; } = new int[17];
        public int[] aISlotShieldRechargeCurrentTime { get; set; } = new int[17];
        public int[] aISlotShieldRechargeRate { get; set; } = new int[17];

        public int[] aISlotWeaponControlAmountOfWeapons { get; set; } = new int[17];

        // weapons

        public int[] aIWeaponSlotExist { get; set; } = new int[5];
        public int[] aIWeaponSlotPowered { get; set; } = new int[5];
        public int[] aIWeaponSlotEnergyRequired { get; set; } = new int[5];
        public int[] aIWeaponSlotDamage { get; set; } = new int[5];
        public int[] aIWeaponSlotReloadTime { get; set; } = new int[5];
        public int[] aIWeaponSlotCurrentReloadTime { get; set; } = new int[5];

        // Crew

        public int[] aICrewExist { get; set; }
        public int[] aICrewHealth { get; set; }
        public int[] aICrewDamage { get; set; }


    }





    /*
     Class - PlayerShip
     */
    //public class PlayerShip : Battle1v1AI
    //{
    //    public PlayerShip()
    //    {
    //    }

    //}

    ///*
    // Class - AiShip
    //*/
    //public class AIShip : Battle1v1AI
    //{
    //    public AIShip()
    //    {
    //    }

    //}

    /*
    Class - slot
    */
    //public class ModuleShipSlot
    //{
    //    public ModuleShipSlot()
    //    {
    //    }

    //  //  public int aIId { get; set; }
    //  //  public int aIShipId { get; set; }
    //}
}
