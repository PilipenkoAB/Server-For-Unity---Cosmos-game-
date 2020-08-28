/*
  Created by Aleksandr Pilipenko

  2020 year
*/


using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Security.Cryptography.X509Certificates;

using System.Threading;
using System.Threading.Tasks;

using System.Data.SQLite;

using System.Security.Cryptography;
using Microsoft.AspNetCore.Cryptography.KeyDerivation;
using System.Diagnostics;
using System.Collections.Generic;
using System.Text;
using System.Data;
using System.Runtime.InteropServices;

namespace Server
{
    /*
      Main class that initiate TCP server and process all responses
    */
    class MainServer
    {
        // Define dictionary with classes of battles
        private static Dictionary<int, Battle1v1AI> sessionsBattle1v1AI = new Dictionary<int, Battle1v1AI>();

        // Define dictionary as cach for logined player
        // key - playerId  values - [sessionToken] \  [battle session]
        private static Dictionary<string, List<string>> playerCache = new Dictionary<string, List<string>>();

        // string connection to the Data Base
        private static string connectionToDBString = "Data Source = DB.db; Version = 3;";

        // port of the server - TCP listener on port 13000.
        private static int port = 13000;

        //BattleTickTime in ms
        public static int battleTickTime = 50;



        // Main 
        static void Main(string[] args)
        {
            // server      and battle
            //  TCPServer();


            Parallel.Invoke(TCPServer, BattleProcess);
            // test();
        }

        static public void BattleProcess()
        {
            //sessionsBattle1v1AI.Add(123, new Battle1v1AI());
            //sessionsBattle1v1AI[123].toStart = 1;
            //sessionsBattle1v1AI[123].playerWeapon1Damage = 6;
            //sessionsBattle1v1AI[123].aiWeapon1Damage = 6;
            //sessionsBattle1v1AI[123].aiWeapon1ReloadCurrent = 4500;
            //sessionsBattle1v1AI[123].playerWeapon1ReloadCurrent = 4600;
            //sessionsBattle1v1AI[123].playerHealthCurrent = 120;
            //sessionsBattle1v1AI[123].aiHealthCurrent = 120;
            //sessionsBattle1v1AI[123].playerWeapon1ReloadTime = 4600;
            //sessionsBattle1v1AI[123].aiWeapon1ReloadTime = 4500;

            //sessionsBattle1v1AI.Add(321, new Battle1v1AI());
            //sessionsBattle1v1AI[321].toStart = 1;
            //sessionsBattle1v1AI[321].playerWeapon1Damage = 4;
            //sessionsBattle1v1AI[321].aiWeapon1Damage = 4;
            //sessionsBattle1v1AI[321].aiWeapon1ReloadCurrent = 5000;
            //sessionsBattle1v1AI[321].playerWeapon1ReloadCurrent = 5000;
            //sessionsBattle1v1AI[321].playerHealthCurrent = 100;
            //sessionsBattle1v1AI[321].aiHealthCurrent = 100;
            //sessionsBattle1v1AI[321].playerWeapon1ReloadTime = 5000;
            //sessionsBattle1v1AI[321].aiWeapon1ReloadTime = 5000;
            //Update DB (closed = 1) - close battle in DB


            while (true)  // check dictionary with battle's all the time for if need to start battle or not
            {
                try
                {
                    // check if battle need to be started
                    foreach (int battleSessionId in sessionsBattle1v1AI.Keys)
                    {
                        if (sessionsBattle1v1AI[battleSessionId].toStart == 1 && sessionsBattle1v1AI[battleSessionId].started == 0 && sessionsBattle1v1AI[battleSessionId].playerReady == 1)
                        {
                            // Process battle
                            sessionsBattle1v1AI[battleSessionId].started = 1;
                            Task.Run(() => {
                                Console.WriteLine("battle session number - " + battleSessionId + " is started");



                                // set start health
                                sessionsBattle1v1AI[battleSessionId].SetStartHealth();


                                //update battle session status every 50ms
                                while (sessionsBattle1v1AI[battleSessionId].finished != 1)
                                {


                                    //  -------------------------- Battle process -----------------------------

                                    // RELOAD
                                    sessionsBattle1v1AI[battleSessionId].ReloadAllWeaponsPerTick();

                                    // ATTACK dummy class
                                    sessionsBattle1v1AI[battleSessionId].AttackDummyClass();




                                    // check if someone dead
                                    if (sessionsBattle1v1AI[battleSessionId].playerShipCurrentHealth <= 0) {
                                        Console.WriteLine("player dead in  session number - " + battleSessionId + "  and it finished");
                                        sessionsBattle1v1AI[battleSessionId].finished = 1;
                                    }
                                    if (sessionsBattle1v1AI[battleSessionId].aIShipCurrentHealth <= 0)
                                    {
                                        Console.WriteLine("ai dead in  session number - " + battleSessionId + "  and it finished");
                                        sessionsBattle1v1AI[battleSessionId].finished = 1;
                                    }

                                    //------------------------- end of battle process --------------------------



                                    // -----------------------------------------------------------comments
                                    //  Console.WriteLine("reload player - "+ sessionsBattle1v1AI[battleSessionId].playerWeapon1ReloadCurrent);
                                    //  Console.WriteLine("reload ai - "+ sessionsBattle1v1AI[battleSessionId].aiWeapon1ReloadCurrent);
                                    //  Console.WriteLine("health player - "+ sessionsBattle1v1AI[battleSessionId].playerHealthCurrent);
                                    //  Console.WriteLine("health ai - "+ sessionsBattle1v1AI[battleSessionId].aiHealthCurrent);
                                    //--------------------------------------------------------------------


                                    // check if time for battle session is up (600000 ms = 10 min)
                                    if (sessionsBattle1v1AI[battleSessionId].battleTime >= 600000)
                                    {
                                        sessionsBattle1v1AI[battleSessionId].finished = 1;
                                        Console.WriteLine("time for session number - " + battleSessionId + " is up and finished");
                                    }


                                    // update time  
                                    //Console.WriteLine(sessionsBattle1v1AI[battleSessionId].battleTime);
                                    sessionsBattle1v1AI[battleSessionId].battleTime += battleTickTime;
                                    Thread.Sleep(battleTickTime);

                                }

                                // if finished - update DB with results
                                if (sessionsBattle1v1AI[battleSessionId].finished == 1)
                                {
                                    Console.WriteLine("update DB after finishin in session " + battleSessionId);

                                    //Update DB (closed = 1) - close battle in DB
                                    string enqueryUpdate = "UPDATE Battle1v1Ai SET Closed = 1 WHERE Battle1v1AiId = @battleSessionId";

                                    using var connectionToDB = new SQLiteConnection(connectionToDBString);
                                    connectionToDB.Open();
                                    using var commandUpdate = new SQLiteCommand(enqueryUpdate, connectionToDB);
                                    commandUpdate.Parameters.AddWithValue("@battleSessionId", battleSessionId);

                                    try
                                    {
                                        commandUpdate.ExecuteNonQuery();
                                        Console.WriteLine("update DB success");

                                        //Delete battle from class dictionary
                                        sessionsBattle1v1AI.Remove(battleSessionId);
                                        if (!sessionsBattle1v1AI.ContainsKey(battleSessionId))
                                        {
                                            Console.WriteLine("deleted succecefully");
                                        }
                                    }
                                    catch
                                    {
                                        Console.WriteLine("update DB FAIL");
                                    }
                                    finally
                                    {
                                        connectionToDB.Close();
                                    }

                                }

                            });
                        }
                    }
                }
                catch
                {
                    Console.WriteLine("BattleProcess main loop error");
                }
                Thread.Sleep(50); // check dictionary every 50 ms (if 0 = 25% processor; if >1 = ~1% processor
            }
        }


        // Process the request from the client
        static private String TCPMessageProcess(String[] recievedMessage)
        {
            string answerToClient = "000";

            // Check if server is online
            if (recievedMessage[0] == "0")
            {
                Console.WriteLine("recieved online check message from client");
                answerToClient = "0";
            }

            // login
            else if (recievedMessage[0] == "1")
            {
                answerToClient = ProcessLoginRequest(recievedMessage);
            }

            // Garage manupulations
            else if (recievedMessage[0] == "2")
            {
                answerToClient = ProcessGarageRequest(recievedMessage);
            }

            // Battle 1v1 AI 
            else if (recievedMessage[0] == "3")
            {
                //Console.WriteLine("Battle 1v1 AI request");

                // from client message -> idmessage ; idplayer ; sessionToken; BattlesessionID; Code;  battle_info
                //                          [0]         [1]           [2]         [3]            [4]    [5..N]

                // player Authorization 
                if (CacheAuthorization(recievedMessage[1], recievedMessage[2]))
                {
                    // check if BattleSessionID exist and playerID is assigned to it
                    if (playerCache[recievedMessage[1]][1] == recievedMessage[3] && recievedMessage[3] != "-1")
                    {
                        // check if battle exist in class dictionary
                        if (sessionsBattle1v1AI.ContainsKey(Convert.ToInt32(recievedMessage[3])))
                        {
                            // require information for UI starting
                            if (recievedMessage[4] == "0")
                            {
                                // prepare infromation to send for UI
                                int playerShipId = sessionsBattle1v1AI[Convert.ToInt32(recievedMessage[3])].playerShipId;
                                int aiShipId = sessionsBattle1v1AI[Convert.ToInt32(recievedMessage[3])].aiShipId;
                                int playerHealthMax = sessionsBattle1v1AI[Convert.ToInt32(recievedMessage[3])].playerShipMaxHealth;
                                int aiHealthMax = sessionsBattle1v1AI[Convert.ToInt32(recievedMessage[3])].aIShipMaxHealth;
                                string playerWeapon1Name = "weapon";
                                int playerWeapon1Damage = 10;
                                int playerWeapon1ReloadTime = sessionsBattle1v1AI[Convert.ToInt32(recievedMessage[3])].playerWeaponSlotReloadTime[0];

                                // answer to client
                                answerToClient = playerShipId + ";" + aiShipId + ";" + playerHealthMax + ";" + aiHealthMax + ";" + playerWeapon1Name + ";" + playerWeapon1Damage + ";" + playerWeapon1ReloadTime;
                            }
                            // Telling server that client is ready and may start battleLoop
                            else if (recievedMessage[4] == "1")
                            {
                                sessionsBattle1v1AI[Convert.ToInt32(recievedMessage[3])].playerReady = 1;
                                // answer to client
                                answerToClient = "answer for 1 - battle loop is started";
                            }
                            // require information for update UI without action
                            else if (recievedMessage[4] == "2")
                            {
                                int battleTime = sessionsBattle1v1AI[Convert.ToInt32(recievedMessage[3])].battleTime;
                                int playerHealthCurrent = sessionsBattle1v1AI[Convert.ToInt32(recievedMessage[3])].playerShipCurrentHealth;
                                int enemyHealthCurrent = sessionsBattle1v1AI[Convert.ToInt32(recievedMessage[3])].aIShipCurrentHealth;
                                int playerWeapon1ReloadCurrent = sessionsBattle1v1AI[Convert.ToInt32(recievedMessage[3])].playerWeaponSlotCurrentReloadTime[0];

                                answerToClient = battleTime + ";" + playerHealthCurrent + ";" + enemyHealthCurrent + ";" + playerWeapon1ReloadCurrent;
                            }
                            // require information for update UI WITH action
                            else if (recievedMessage[4] == "3")
                            {
                                // code system of pressed buttons
                                // ????????????????????
                                if (recievedMessage[5] == "0")
                                {
                                    // TEST ONE - pressed only button attack weapon1
                                    sessionsBattle1v1AI[Convert.ToInt32(recievedMessage[3])].PlayerAttackWeapon();


                                    // answer to client  - 0 means that action was successeful
                                    answerToClient = "0";
                                }
                                else if (recievedMessage[5] == "1")
                                {
                                    answerToClient = "0";
                                }

                            }

                        }
                        else
                        {
                            // NEED TO FIX!!!! 
                            // ???? when battle is ended = 111
                            answerToClient = "111";

                            Console.WriteLine("Battle 1v1 AI request - player's battle id is not exist in class dictionary");
                        }



                    }
                    else
                    {
                        Console.WriteLine("Battle 1v1 AI request - player's battle session token is wrong");
                    }
                }
            }

            // Error response
            else
            {
                Console.WriteLine("error package from client");
                answerToClient = "000";
            }

            return answerToClient;
        }


        // Process answer from client

        // Login 
        static private string ProcessLoginRequest(String[] recievedMessage) {

            string login = recievedMessage[1];
            string password = recievedMessage[2];
            string hashedPasswordFromDB = "";
            string playerId = "";
            string answerToClient = "";

            Console.WriteLine("Recieved login require from " + login);

            string queryString = "SELECT Password  FROM Account where Login = @login ";
            string[,] queryParameters = new string[,] { { "login", login } };
            string[] stringType = new string[] { "string" };

            hashedPasswordFromDB = RequestToGetValueFromDB(queryString, stringType, queryParameters)[0][0];

            if (hashedPasswordFromDB == null)
            {
                Console.WriteLine("Player with that Login - " + login + " does not exist!");
                answerToClient = "000";
            }
            else
            {
                using var connectionToDB = new SQLiteConnection(connectionToDBString);
                connectionToDB.Open();

                //

                string hashedPasswordFromPlayer = PasswordHash(password);  // getting hashed password

                // Update player information - add session token and set start ship slot == 0
                if (hashedPasswordFromPlayer == hashedPasswordFromDB)
                {
                    var rand = new Random();
                    int randomNumber = rand.Next(000000, 999999);

                    string sessionToken = login + Convert.ToString(randomNumber);
                    //Console.WriteLine("session token = " + sessionToken);

                    // add information to the DB
                    string enqueryUpdate = "UPDATE Account SET SessionToken = '" + sessionToken + "', GarageActiveSlot = 0  WHERE Login = '" + login + "'";

                    using var commandUpdate = new SQLiteCommand(enqueryUpdate, connectionToDB);

                    try
                    {
                        commandUpdate.ExecuteNonQuery();

                        // get Player ID
                        queryString = "SELECT AccountId  FROM Account where Login = @login AND SessionToken = @sessionToken";
                        queryParameters = new string[,] { { "login", login }, { "sessionToken", sessionToken } };
                        stringType = new string[] { "int" };

                        playerId = RequestToGetValueFromDB(queryString, stringType, queryParameters)[0][0];

                        // check if cache for player is exist or not. if exist - update session token, if not exist - create new cache information
                        if (playerCache.ContainsKey(playerId))
                        {
                            playerCache[playerId][0] = sessionToken;
                        }
                        else
                        {
                            // crete cache for keeping login information for all connections after login 
                            List<string> cacheLoginList = new List<string>();
                            cacheLoginList.Add(sessionToken); // session token
                            cacheLoginList.Add("-1");          // battle session

                            playerCache.Add(playerId, cacheLoginList);
                        }
                        Console.WriteLine("Player - " + login + " login SUCCESSEFUL");

                        answerToClient = playerId + ";" + sessionToken;
                    }
                    catch
                    {
                        Console.WriteLine("ERROR updating login table with session token and creation cache information");
                    }
                }
                else
                {
                    Console.WriteLine("login UNSUCCESSEFUL (pass incorrect)");
                    answerToClient = "001";
                }

                connectionToDB.Close();
            }

            //     Console.WriteLine("!debug! answer to the clien" + answerToClient);
            return answerToClient;
        }

        // Garage
        static private string ProcessGarageRequest(String[] recievedMessage)
        {
            string answerToClient = "";

            string playerId = recievedMessage[1];
            string sessionToken = recievedMessage[2];


            if (CacheAuthorization(playerId, sessionToken))
            {
                answerToClient = ProcessGarageRequestAfterAuthorization(recievedMessage);
            }

            Console.WriteLine("Garage request for cache authorization from - " + playerId + " with session token - " + sessionToken + " . answer - " + answerToClient);

            return answerToClient;
        }

        static private string ProcessGarageRequestAfterAuthorization(String[] recievedMessage) {

            string answerToClient = "";

            string playerId = recievedMessage[1];
            string codeActivity = recievedMessage[3];


            if (codeActivity == "0")
            {
                if (recievedMessage[4] == "0")
                {
                    answerToClient = ReceiveGarageMainInformation(recievedMessage);
                    Console.WriteLine("DEBUG - 1 - " + recievedMessage[4]);
                }
                else
                {
                    answerToClient = RecieveNewShipScrollInformation(recievedMessage);
                    Console.WriteLine("DEBUG - 2 - " + recievedMessage[4]);
                }

            }
            else if (codeActivity == "1")
            {
                Console.WriteLine("Battle 1v1 AI");

                string sessionID = StartSession1v1AI(Convert.ToInt32(playerId));

                // add information about the sessionBattleID to Server Battle cache
                if (playerCache.ContainsKey(playerId))
                {
                    playerCache[playerId][1] = sessionID;
                }
                answerToClient = sessionID;
            }
            else if (codeActivity == "2")
            {
                answerToClient = RecieveGarageInventory(recievedMessage);
            }
            return answerToClient;
        }

        // code activite = 0 from ProcessGarageRequestAfterAuthorization
        // return AccountItemId (item connected to account) and ItemId (item Id)
        static private string ReceiveGarageMainInformation(String[] recievedMessage)
        {
            string answerToClient = "";

            string playerId = recievedMessage[1];

            // modules for answer start information
            // id of the item in the account-item system
            int accountEngineSlot = -1;
            int accountCockpitSlot = -1;
            int[] accountBigSlot = new int[] { -1, -1, -1, -1, -1 };
            int[] accountMediumSlot = new int[] { -1, -1, -1, -1, -1 };
            int[] accountSmallSlot = new int[] { -1, -1, -1, -1, -1 };
            int[] accountWeapon = new int[] { -1, -1, -1, -1, -1 };

            // ID of the slot in item system
            //  -1 = does not exist, 0 - empty , n - something - in account
            int engineSlotId = 0;
            int cockpitSlotId = 0;
            int[] bigSlotId = new int[] { 0, 0, 0, 0, 0 };
            int[] mediumSlotId = new int[] { 0, 0, 0, 0, 0 };
            int[] smallSlotId = new int[] { 0, 0, 0, 0, 0 };
            int[] weaponId = new int[] { 0, 0, 0, 0, 0 };

            string[] crew = new string[] { "0", "0", "0", "0", "0", "0", "0", "0", "0", "0", "0", "0", "0", "0", "0" };

            int[] slotShip = new int[] { 0, 0, 0 }; // middle, right, left
            int[] slotIdInfo = new int[] { 0, 0, 0 }; // middle, right, left


            //-------------------------------------------------------------
            // SLOTS

            //   MIDDLE SLOT

            // does not looks like a good query, because of doubling some positions (TO FIX)
            string queryString = @"SELECT Garage.slot, AccountShip.ShipId, Garage.AccountShipId 
                            FROM Garage, AccountShip 
                            WHERE Garage.AccountId = @playerID
                            AND (Garage.AccountShipId = AccountShip.AccountShipId AND Garage.Slot == 0)
                            ORDER BY Garage.slot ASC";
            string[,] queryParameters = new string[,] { { "playerId", playerId } };
            string[] stringType = new string[] { "int", "int", "int" };
            List<string>[] requestAnswer = RequestToGetValueFromDB(queryString, stringType, queryParameters);

            List<string> slots = requestAnswer[0];
            List<string> shipsID = requestAnswer[1];
            List<string> accountShipId = requestAnswer[2];

            Console.WriteLine("DEBUG - " + requestAnswer[0].Count);
            if (shipsID.Count > 0)
            {
                slotShip[0] = Convert.ToInt32(shipsID[0]);
                slotIdInfo[0] = Convert.ToInt32(slots[0]);
            }
            else
            {
                slotShip[0] = 0;
                slotIdInfo[0] = 0;
            }
            Console.WriteLine("DEBUG slot  - " + slotIdInfo[0]);



            // RIGHT SLOT

            // does not looks like a good query, because of doubling some positions (TO FIX)
            queryString = @"SELECT Garage.slot, AccountShip.ShipId, Garage.AccountShipId 
                            FROM Garage, AccountShip 
                            WHERE Garage.AccountId = @playerID
                            AND (Garage.AccountShipId = AccountShip.AccountShipId AND Garage.Slot = 1)
                            ORDER BY Garage.slot ASC";
            queryParameters = new string[,] { { "playerId", playerId } };
            stringType = new string[] { "int", "int", "int" };
            requestAnswer = RequestToGetValueFromDB(queryString, stringType, queryParameters);

            slots = requestAnswer[0];
            shipsID = requestAnswer[1];

            Console.WriteLine("DEBUG - " + requestAnswer[0].Count);
            if (shipsID.Count > 0)
            {
                slotShip[1] = Convert.ToInt32(shipsID[0]);
                slotIdInfo[1] = Convert.ToInt32(slots[0]);
            }
            else
            {
                slotShip[1] = 0;
                slotIdInfo[1] = 0;
            }
            Console.WriteLine("DEBUG slot  - " + slotIdInfo[1]);


            // LEFT SLOT
            // get all amount of slots user has
            queryString = @"SELECT COUNT(Garage.slot )
                            FROM Garage
                            WHERE Garage.AccountId = @playerID
                            ORDER BY Garage.slot ASC";
            queryParameters = new string[,] { { "playerId", playerId } };
            stringType = new string[] { "int" };
            requestAnswer = RequestToGetValueFromDB(queryString, stringType, queryParameters);


            int leftSlot = Convert.ToInt32(requestAnswer[0][0]) - 1;

            // does not looks like a good query, because of doubling some positions (TO FIX)
            queryString = @"SELECT Garage.slot, AccountShip.ShipId, Garage.AccountShipId 
                            FROM Garage, AccountShip 
                            WHERE Garage.AccountId = @playerID
                            AND (Garage.AccountShipId = AccountShip.AccountShipId AND Garage.Slot = @rightSlot)
                            ORDER BY Garage.slot ASC";
            queryParameters = new string[,] { { "playerId", playerId }, { "rightSlot", Convert.ToString(leftSlot) } };
            stringType = new string[] { "int", "int", "int" };
            requestAnswer = RequestToGetValueFromDB(queryString, stringType, queryParameters);

            slots = requestAnswer[0];
            shipsID = requestAnswer[1];

            Console.WriteLine("DEBUG - " + requestAnswer[1].Count);
            if (shipsID.Count > 0)
            {
                slotShip[2] = Convert.ToInt32(shipsID[0]);
                slotIdInfo[2] = Convert.ToInt32(slots[0]);
            }
            else
            {
                slotShip[2] = 0;
                slotIdInfo[2] = leftSlot;
            }
            Console.WriteLine("DEBUG slot  - " + slotIdInfo[2]);


            //-------------------------------------------------------------








            //-------------------------------------------------------

            queryString = @"SELECT AccountShip.AccountShipId, AccountShip.EngineSlot, AccountShip.CockpitSlot, AccountShip.BigSlot1, AccountShip.BigSlot2,
                                     AccountShip.BigSlot3, AccountShip.BigSlot4, AccountShip.BigSlot5, AccountShip.MediumSlot1, AccountShip.MediumSlot2,
                                     AccountShip.MediumSlot3, AccountShip.MediumSlot4, AccountShip.MediumSlot5, AccountShip.SmallSlot1, AccountShip.SmallSlot2,
                                     AccountShip.SmallSlot3, AccountShip.SmallSlot4, AccountShip.SmallSlot5,
                                     AccountShip.Weapon1, AccountShip.Weapon2, AccountShip.Weapon3, AccountShip.Weapon4, AccountShip.Weapon5
                            FROM AccountShip WHERE AccountShip.AccountShipId = @accountShipId";
            queryParameters = new string[,] { { "accountShipId", accountShipId[0] } };
            stringType = new string[] { "int", "int", "int", "int", "int", "int", "int", "int", "int", "int", "int", "int", "int", "int", "int", "int", "int", "int", "int", "int", "int", "int", "int" };
            requestAnswer = RequestToGetValueFromDB(queryString, stringType, queryParameters);

            accountEngineSlot = Convert.ToInt32(requestAnswer[1][0]);
            accountCockpitSlot = Convert.ToInt32(requestAnswer[2][0]);
            accountBigSlot[0] = Convert.ToInt32(requestAnswer[3][0]);
            accountBigSlot[1] = Convert.ToInt32(requestAnswer[4][0]);
            accountBigSlot[2] = Convert.ToInt32(requestAnswer[5][0]);
            accountBigSlot[3] = Convert.ToInt32(requestAnswer[6][0]);
            accountBigSlot[4] = Convert.ToInt32(requestAnswer[7][0]);
            accountMediumSlot[0] = Convert.ToInt32(requestAnswer[8][0]);
            accountMediumSlot[1] = Convert.ToInt32(requestAnswer[9][0]);
            accountMediumSlot[2] = Convert.ToInt32(requestAnswer[10][0]);
            accountMediumSlot[3] = Convert.ToInt32(requestAnswer[11][0]);
            accountMediumSlot[4] = Convert.ToInt32(requestAnswer[12][0]);
            accountSmallSlot[0] = Convert.ToInt32(requestAnswer[13][0]);
            accountSmallSlot[1] = Convert.ToInt32(requestAnswer[14][0]);
            accountSmallSlot[2] = Convert.ToInt32(requestAnswer[15][0]);
            accountSmallSlot[3] = Convert.ToInt32(requestAnswer[16][0]);
            accountSmallSlot[4] = Convert.ToInt32(requestAnswer[17][0]);
            accountWeapon[0] = Convert.ToInt32(requestAnswer[18][0]);
            accountWeapon[1] = Convert.ToInt32(requestAnswer[19][0]);
            accountWeapon[2] = Convert.ToInt32(requestAnswer[20][0]);
            accountWeapon[3] = Convert.ToInt32(requestAnswer[21][0]);
            accountWeapon[4] = Convert.ToInt32(requestAnswer[22][0]);



            // check system if some module installed - what id of the module 
            if (accountEngineSlot > 0)
            {
                queryString = @"SELECT Engine.EngineId
                                FROM AccountShip, AccountItem, Engine
                                WHERE AccountShip.AccountShipId = @accountShipId 
                                and AccountShip.EngineSlot = AccountItem.AccountItemId 
                                and AccountItem.EngineId = Engine.EngineId";
                queryParameters = new string[,] { { "accountShipId", accountShipId[0] } };
                stringType = new string[] { "int" };
                requestAnswer = RequestToGetValueFromDB(queryString, stringType, queryParameters);
                engineSlotId = Convert.ToInt32(requestAnswer[0][0]);
            }

            if (accountCockpitSlot > 0)
            {
                queryString = @"SELECT Cockpit.CockpitId
                               FROM AccountShip, AccountItem, Cockpit
                               WHERE AccountShip.AccountShipId = @accountShipId
                               and AccountShip.CockpitSlot = AccountItem.AccountItemId 
                               and AccountItem.CockpitId = Cockpit.CockpitId";
                queryParameters = new string[,] { { "accountShipId", accountShipId[0] } };
                stringType = new string[] { "int" };
                requestAnswer = RequestToGetValueFromDB(queryString, stringType, queryParameters);
                cockpitSlotId = Convert.ToInt32(requestAnswer[0][0]);
            }

            for (int i = 0; i < accountBigSlot.Length; i++)
            {
                if (accountBigSlot[i] > 0)
                {
                    queryString = @"SELECT BigSlot.BigSlotId
                               FROM AccountShip, AccountItem, BigSlot
                               WHERE AccountShip.AccountShipId = @accountShipId
                               and AccountShip.BigSlot" + (i + 1) + @" = AccountItem.AccountItemId 
                               and AccountItem.BigSlotId = BigSlot.BigSlotId";
                    queryParameters = new string[,] { { "accountShipId", accountShipId[0] } };
                    stringType = new string[] { "int" };
                    requestAnswer = RequestToGetValueFromDB(queryString, stringType, queryParameters);
                    bigSlotId[i] = Convert.ToInt32(requestAnswer[0][0]);
                }
            }

            for (int i = 0; i < accountMediumSlot.Length; i++)
            {
                if (accountMediumSlot[i] > 0)
                {
                }
            }

            for (int i = 0; i < accountSmallSlot.Length; i++)
            {
                if (accountSmallSlot[i] > 0)
                {
                }
            }

            for (int i = 0; i < accountWeapon.Length; i++)
            {
                if (accountWeapon[i] > 0)
                {
                    int weaponNumber = i + 1;
                    queryString = @"SELECT Weapon.WeaponId
                                    FROM AccountShip, AccountItem, Weapon
                                    WHERE AccountShip.AccountShipId = @accountShipId
                                    and AccountShip.Weapon" + weaponNumber + @" = AccountItem.AccountItemId
                                    and AccountItem.WeaponId = Weapon.WeaponId";
                    queryParameters = new string[,] { { "accountShipId", accountShipId[0] } };
                    stringType = new string[] { "int" };
                    requestAnswer = RequestToGetValueFromDB(queryString, stringType, queryParameters);
                    weaponId[i] = Convert.ToInt32(requestAnswer[0][0]);
                }
            }



            // CREWinformation
            queryString = @"SELECT AccountCrew.CrewId
                            FROM AccountCrew
                            WHERE AccountCrew.AccountId = @playerID
                            AND  AccountCrew.AccountShipId = @accountShipId";
            queryParameters = new string[,] { { "playerId", playerId }, { "accountShipId", accountShipId[0] } };
            stringType = new string[] { "int" };
            requestAnswer = RequestToGetValueFromDB(queryString, stringType, queryParameters);

            for (int i = 0; i < requestAnswer[0].Count; i++)
            {
                crew[i] = requestAnswer[0][i];
            }




            // Answer should include AccountItemId (for future manupulations with item) and ItemId 
            //(for big slot - does not matter if shield of etc, because DB system duplicated in the client)
            answerToClient = slotShip[0] + ";" + slotShip[1] + ";" + slotShip[2] +
                 ";" + accountEngineSlot + ";" + engineSlotId + ";" + accountCockpitSlot + ";" + cockpitSlotId +
                 ";" + accountBigSlot[0] + ";" + bigSlotId[0] + ";" + accountBigSlot[1] + ";" + bigSlotId[1] +
                 ";" + accountBigSlot[2] + ";" + bigSlotId[2] + ";" + accountBigSlot[3] + ";" + bigSlotId[3] +
                 ";" + accountBigSlot[4] + ";" + bigSlotId[4] +
                 ";" + accountMediumSlot[0] + ";" + mediumSlotId[0] + ";" + accountMediumSlot[1] + ";" + mediumSlotId[1] +
                 ";" + accountMediumSlot[2] + ";" + mediumSlotId[2] + ";" + accountMediumSlot[3] + ";" + mediumSlotId[3] +
                 ";" + accountMediumSlot[4] + ";" + mediumSlotId[4] +
                 ";" + accountSmallSlot[0] + ";" + smallSlotId[0] + ";" + accountSmallSlot[1] + ";" + smallSlotId[1] +
                 ";" + accountSmallSlot[2] + ";" + smallSlotId[2] + ";" + accountSmallSlot[3] + ";" + smallSlotId[3] +
                 ";" + accountSmallSlot[4] + ";" + smallSlotId[4] +
                 ";" + accountWeapon[0] + ";" + weaponId[0] + ";" + accountWeapon[1] + ";" + weaponId[1] +
                 ";" + accountWeapon[2] + ";" + weaponId[2] + ";" + accountWeapon[3] + ";" + weaponId[3] +
                 ";" + accountWeapon[4] + ";" + weaponId[4] +
                 ";" + slotIdInfo[0] + ";" + slotIdInfo[1] + ";" + slotIdInfo[2] +
                 ";" + crew[0] + ";" + crew[1] + ";" + crew[2] + ";" + crew[3] + ";" + crew[4] + ";" + crew[5] +
                 ";" + crew[6] + ";" + crew[7] + ";" + crew[8] + ";" + crew[9] + ";" + crew[10] + ";" + crew[11] +
                 ";" + crew[12] + ";" + crew[13] + ";" + crew[14];

            return answerToClient;
        }

        static private string RecieveNewShipScrollInformation(String[] recievedMessage)
        {
            string answerToClient = "";

            string playerId = recievedMessage[1];
            string middleslotShipId = recievedMessage[4];
            string slotShipId = recievedMessage[4];

            string shipIdtoClient;

            // modules for answer start information
            // id of the item in the account-item system
            int accountEngineSlot = -1;
            int accountCockpitSlot = -1;
            int[] accountBigSlot = new int[] { -1, -1, -1, -1, -1 };
            int[] accountMediumSlot = new int[] { -1, -1, -1, -1, -1 };
            int[] accountSmallSlot = new int[] { -1, -1, -1, -1, -1 };
            int[] accountWeapon = new int[] { -1, -1, -1, -1, -1 };

            // ID of the slot in item system
            //  -1 = does not exist, 0 - empty , n - something - in account
            int engineSlotId = 0;
            int cockpitSlotId = 0;
            int[] bigSlotId = new int[] { 0, 0, 0, 0, 0 };
            int[] mediumSlotId = new int[] { 0, 0, 0, 0, 0 };
            int[] smallSlotId = new int[] { 0, 0, 0, 0, 0 };
            int[] weaponId = new int[] { 0, 0, 0, 0, 0 };

            string[] crew = new string[] { "0", "0", "0", "0", "0", "0", "0", "0", "0", "0", "0", "0", "0", "0", "0" };

            ///-------------------------------
            ///
            int[] slotShip = new int[] { 0, 0, 0 }; // middle, right, left
            int[] slotIdInfo = new int[] { 0, 0, 0 }; // middle, right, left

            // does not looks like a good query, because of doubling some positions (TO FIX)
            string queryString = @"SELECT Garage.slot, AccountShip.ShipId, Garage.AccountShipId 
                            FROM Garage, AccountShip 
                            WHERE Garage.AccountId = @playerID
                            AND (Garage.Slot = @slotShipId) AND Garage.AccountShipId = AccountShip.AccountShipId
                            ORDER BY Garage.slot ASC";
            string[,] queryParameters = new string[,] { { "playerId", playerId }, { "slotShipId", slotShipId } };
            string[] stringType = new string[] { "int", "int", "int" };
            List<string>[] requestAnswer = RequestToGetValueFromDB(queryString, stringType, queryParameters);

            List<string> slots = requestAnswer[0];
            List<string> shipsID = requestAnswer[1];
            List<string> accountShipId = requestAnswer[2];

            if (shipsID.Count > 0)
            {
                slotShip[0] = Convert.ToInt32(shipsID[0]);
                slotIdInfo[0] = Convert.ToInt32(slots[0]);
                shipIdtoClient = accountShipId[0];
            }
            else
            {
                slotShip[0] = 0;
                slotIdInfo[0] = Convert.ToInt32(slotShipId);
                shipIdtoClient = "0";
            }

            Console.WriteLine("DEBUG slot middle  - " + slotIdInfo[0] + " shipID - " + slotShip[0]);




            //----------------- right
            // does not looks like a good query, because of doubling some positions (TO FIX)
            queryString = @"SELECT COUNT(Garage.slot )
                            FROM Garage
                            WHERE Garage.AccountId = @playerID
                            ORDER BY Garage.slot ASC";
            queryParameters = new string[,] { { "playerId", playerId } };
            stringType = new string[] { "int" };
            requestAnswer = RequestToGetValueFromDB(queryString, stringType, queryParameters);


            int maxSlot = Convert.ToInt32(requestAnswer[0][0]) - 1;


            if (Convert.ToInt32(middleslotShipId) == maxSlot)
            {
                slotShipId = "0";
            }
            else
            {
                slotShipId = Convert.ToString(Convert.ToInt32(slotShipId) + 1);
            }
            Console.WriteLine("right slotShipId !!!!! - " + slotShipId);


            queryString = @"SELECT Garage.slot, AccountShip.ShipId, Garage.AccountShipId 
                            FROM Garage, AccountShip 
                            WHERE Garage.AccountId = @playerID
                            AND (Garage.Slot = @slotShipId) AND Garage.AccountShipId = AccountShip.AccountShipId
                            ORDER BY Garage.slot ASC";
            queryParameters = new string[,] { { "playerId", playerId }, { "slotShipId", slotShipId } };
            stringType = new string[] { "int", "int", "int" };
            requestAnswer = RequestToGetValueFromDB(queryString, stringType, queryParameters);

            slots = requestAnswer[0];
            shipsID = requestAnswer[1];

            if (shipsID.Count > 0)
            {
                slotShip[1] = Convert.ToInt32(shipsID[0]);
                slotIdInfo[1] = Convert.ToInt32(slots[0]);
            }
            else
            {
                slotShip[1] = 0;
                slotIdInfo[1] = Convert.ToInt32(slotShipId);
            }

            Console.WriteLine("DEBUG slot right  - " + slotIdInfo[1] + " shipID - " + slotShip[1]);



            //------------------left
            if (Convert.ToInt32(middleslotShipId) == 0)
            {
                slotShipId = Convert.ToString(maxSlot);
            }
            else
            {
                slotShipId = Convert.ToString(Convert.ToInt32(middleslotShipId) - 1);
            }

            queryString = @"SELECT Garage.slot, AccountShip.ShipId, Garage.AccountShipId 
                            FROM Garage, AccountShip 
                            WHERE Garage.AccountId = @playerID
                            AND (Garage.Slot = @slotShipId) AND Garage.AccountShipId = AccountShip.AccountShipId
                            ORDER BY Garage.slot ASC";
            queryParameters = new string[,] { { "playerId", playerId }, { "slotShipId", slotShipId } };
            stringType = new string[] { "int", "int", "int" };
            requestAnswer = RequestToGetValueFromDB(queryString, stringType, queryParameters);

            slots = requestAnswer[0];
            shipsID = requestAnswer[1];

            if (shipsID.Count > 0)
            {
                slotShip[2] = Convert.ToInt32(shipsID[0]);
                slotIdInfo[2] = Convert.ToInt32(slots[0]);
            }
            else
            {
                slotShip[2] = 0;
                slotIdInfo[2] = Convert.ToInt32(slotShipId);
            }

            Console.WriteLine("DEBUG slot left  - " + slotIdInfo[2] + " shipID - " + slotShip[2]);
            //-------------------------------------



            if (shipIdtoClient != "0")
            {

                queryString = @"SELECT AccountShip.AccountShipId, AccountShip.EngineSlot, AccountShip.CockpitSlot, AccountShip.BigSlot1, AccountShip.BigSlot2,
                                     AccountShip.BigSlot3, AccountShip.BigSlot4, AccountShip.BigSlot5, AccountShip.MediumSlot1, AccountShip.MediumSlot2,
                                     AccountShip.MediumSlot3, AccountShip.MediumSlot4, AccountShip.MediumSlot5, AccountShip.SmallSlot1, AccountShip.SmallSlot2,
                                     AccountShip.SmallSlot3, AccountShip.SmallSlot4, AccountShip.SmallSlot5,
                                     AccountShip.Weapon1, AccountShip.Weapon2, AccountShip.Weapon3, AccountShip.Weapon4, AccountShip.Weapon5
                            FROM AccountShip WHERE AccountShip.AccountShipId = @accountShipId";
                queryParameters = new string[,] { { "accountShipId", accountShipId[0] } };
                stringType = new string[] { "int", "int", "int", "int", "int", "int", "int", "int", "int", "int", "int", "int", "int", "int", "int", "int", "int", "int", "int", "int", "int", "int", "int" };
                requestAnswer = RequestToGetValueFromDB(queryString, stringType, queryParameters);

                accountEngineSlot = Convert.ToInt32(requestAnswer[1][0]);
                accountCockpitSlot = Convert.ToInt32(requestAnswer[2][0]);
                accountBigSlot[0] = Convert.ToInt32(requestAnswer[3][0]);
                accountBigSlot[1] = Convert.ToInt32(requestAnswer[4][0]);
                accountBigSlot[2] = Convert.ToInt32(requestAnswer[5][0]);
                accountBigSlot[3] = Convert.ToInt32(requestAnswer[6][0]);
                accountBigSlot[4] = Convert.ToInt32(requestAnswer[7][0]);
                accountMediumSlot[0] = Convert.ToInt32(requestAnswer[8][0]);
                accountMediumSlot[1] = Convert.ToInt32(requestAnswer[9][0]);
                accountMediumSlot[2] = Convert.ToInt32(requestAnswer[10][0]);
                accountMediumSlot[3] = Convert.ToInt32(requestAnswer[11][0]);
                accountMediumSlot[4] = Convert.ToInt32(requestAnswer[12][0]);
                accountSmallSlot[0] = Convert.ToInt32(requestAnswer[13][0]);
                accountSmallSlot[1] = Convert.ToInt32(requestAnswer[14][0]);
                accountSmallSlot[2] = Convert.ToInt32(requestAnswer[15][0]);
                accountSmallSlot[3] = Convert.ToInt32(requestAnswer[16][0]);
                accountSmallSlot[4] = Convert.ToInt32(requestAnswer[17][0]);
                accountWeapon[0] = Convert.ToInt32(requestAnswer[18][0]);
                accountWeapon[1] = Convert.ToInt32(requestAnswer[19][0]);
                accountWeapon[2] = Convert.ToInt32(requestAnswer[20][0]);
                accountWeapon[3] = Convert.ToInt32(requestAnswer[21][0]);
                accountWeapon[4] = Convert.ToInt32(requestAnswer[22][0]);



                // check system if some module installed - what id of the module 
                if (accountEngineSlot > 0)
                {
                    queryString = @"SELECT Engine.EngineId
                                FROM AccountShip, AccountItem, Engine
                                WHERE AccountShip.AccountShipId = @accountShipId 
                                and AccountShip.EngineSlot = AccountItem.AccountItemId 
                                and AccountItem.EngineId = Engine.EngineId";
                    queryParameters = new string[,] { { "accountShipId", accountShipId[0] } };
                    stringType = new string[] { "int" };
                    requestAnswer = RequestToGetValueFromDB(queryString, stringType, queryParameters);
                    engineSlotId = Convert.ToInt32(requestAnswer[0][0]);
                }

                if (accountCockpitSlot > 0)
                {
                    queryString = @"SELECT Cockpit.CockpitId
                               FROM AccountShip, AccountItem, Cockpit
                               WHERE AccountShip.AccountShipId = @accountShipId
                               and AccountShip.CockpitSlot = AccountItem.AccountItemId 
                               and AccountItem.CockpitId = Cockpit.CockpitId";
                    queryParameters = new string[,] { { "accountShipId", accountShipId[0] } };
                    stringType = new string[] { "int" };
                    requestAnswer = RequestToGetValueFromDB(queryString, stringType, queryParameters);
                    cockpitSlotId = Convert.ToInt32(requestAnswer[0][0]);
                }

                for (int i = 0; i < accountBigSlot.Length; i++)
                {
                    if (accountBigSlot[i] > 0)
                    {
                        queryString = @"SELECT BigSlot.BigSlotId
                               FROM AccountShip, AccountItem, BigSlot
                               WHERE AccountShip.AccountShipId = @accountShipId
                               and AccountShip.BigSlot" + (i + 1) + @" = AccountItem.AccountItemId 
                               and AccountItem.BigSlotId = BigSlot.BigSlotId";
                        queryParameters = new string[,] { { "accountShipId", accountShipId[0] } };
                        stringType = new string[] { "int" };
                        requestAnswer = RequestToGetValueFromDB(queryString, stringType, queryParameters);
                        bigSlotId[i] = Convert.ToInt32(requestAnswer[0][0]);
                    }
                }

                for (int i = 0; i < accountMediumSlot.Length; i++)
                {
                    if (accountMediumSlot[i] > 0)
                    {
                    }
                }

                for (int i = 0; i < accountSmallSlot.Length; i++)
                {
                    if (accountSmallSlot[i] > 0)
                    {
                    }
                }

                for (int i = 0; i < accountWeapon.Length; i++)
                {
                    if (accountWeapon[i] > 0)
                    {
                        int weaponNumber = i + 1;
                        queryString = @"SELECT Weapon.WeaponId
                                    FROM AccountShip, AccountItem, Weapon
                                    WHERE AccountShip.AccountShipId = @accountShipId
                                    and AccountShip.Weapon" + weaponNumber + @" = AccountItem.AccountItemId
                                    and AccountItem.WeaponId = Weapon.WeaponId";
                        queryParameters = new string[,] { { "accountShipId", accountShipId[0] } };
                        stringType = new string[] { "int" };
                        requestAnswer = RequestToGetValueFromDB(queryString, stringType, queryParameters);
                        weaponId[i] = Convert.ToInt32(requestAnswer[0][0]);
                    }
                }

                // CREWinformation
                queryString = @"SELECT AccountCrew.CrewId
                            FROM AccountCrew
                            WHERE AccountCrew.AccountId = @playerID
                            AND  AccountCrew.AccountShipId = @accountShipId";
                queryParameters = new string[,] { { "playerId", playerId }, { "accountShipId", accountShipId[0] } };
                stringType = new string[] { "int" };
                requestAnswer = RequestToGetValueFromDB(queryString, stringType, queryParameters);

                for (int i = 0; i < requestAnswer[0].Count; i++)
                {
                    crew[i] = requestAnswer[0][i];
                }


            }


            // Answer should include AccountItemId (for future manupulations with item) and ItemId 
            //(for big slot - does not matter if shield of etc, because DB system duplicated in the client)
            answerToClient = slotShip[0] + ";" + slotShip[1] + ";" + slotShip[2] +
                 ";" + accountEngineSlot + ";" + engineSlotId + ";" + accountCockpitSlot + ";" + cockpitSlotId +
                 ";" + accountBigSlot[0] + ";" + bigSlotId[0] + ";" + accountBigSlot[1] + ";" + bigSlotId[1] +
                 ";" + accountBigSlot[2] + ";" + bigSlotId[2] + ";" + accountBigSlot[3] + ";" + bigSlotId[3] +
                 ";" + accountBigSlot[4] + ";" + bigSlotId[4] +
                 ";" + accountMediumSlot[0] + ";" + mediumSlotId[0] + ";" + accountMediumSlot[1] + ";" + mediumSlotId[1] +
                 ";" + accountMediumSlot[2] + ";" + mediumSlotId[2] + ";" + accountMediumSlot[3] + ";" + mediumSlotId[3] +
                 ";" + accountMediumSlot[4] + ";" + mediumSlotId[4] +
                 ";" + accountSmallSlot[0] + ";" + smallSlotId[0] + ";" + accountSmallSlot[1] + ";" + smallSlotId[1] +
                 ";" + accountSmallSlot[2] + ";" + smallSlotId[2] + ";" + accountSmallSlot[3] + ";" + smallSlotId[3] +
                 ";" + accountSmallSlot[4] + ";" + smallSlotId[4] +
                 ";" + accountWeapon[0] + ";" + weaponId[0] + ";" + accountWeapon[1] + ";" + weaponId[1] +
                 ";" + accountWeapon[2] + ";" + weaponId[2] + ";" + accountWeapon[3] + ";" + weaponId[3] +
                 ";" + accountWeapon[4] + ";" + weaponId[4] +
                 ";" + slotIdInfo[0] + ";" + slotIdInfo[1] + ";" + slotIdInfo[2] +
                 ";" + crew[0] + ";" + crew[1] + ";" + crew[2] + ";" + crew[3] + ";" + crew[4] + ";" + crew[5] +
                 ";" + crew[6] + ";" + crew[7] + ";" + crew[8] + ";" + crew[9] + ";" + crew[10] + ";" + crew[11] +
                 ";" + crew[12] + ";" + crew[13] + ";" + crew[14];

            return answerToClient;
        }


        static private void GetInformationForTheGarage()
        {


        }
        // code activite = 2 from ProcessGarageRequestAfterAuthorization
        static private string RecieveGarageInventory(String[] recievedMessage)
        {
            string answerToClient = "";

            string playerId = recievedMessage[1];

            Console.WriteLine("Inventory information ");
            string answerType = "";
            string answerItemTypeId = "";

            string queryString = "SELECT * FROM AccountItem WHERE AccountItem.AccountId = @playerID";
            string[,] queryParameters = new string[,] { { "playerID", playerId } };
            string[] stringType = new string[] { "int", "int", "int", "int", "int", "int", "int", "int", "int", "int" };
            List<string>[] requestAnswer = RequestToGetValueFromDB(queryString, stringType, queryParameters);

            // starts from 4 - because item type starts from 4 in DB
            for (int i = 0; i < requestAnswer[0].Count; i++)
            {
                for (int ii = 0; ii < 6; ii++)
                {
                    if (Convert.ToInt32(requestAnswer[ii + 4][i]) != 0)
                    {
                        answerType = "" + ii;
                        answerItemTypeId = requestAnswer[ii + 4][i];
                    }
                }
                answerToClient = answerToClient + requestAnswer[0][0] + ";" + answerType + ";" + answerItemTypeId + ";";
            }
            answerToClient = answerToClient.Remove(answerToClient.Length - 1);

            return answerToClient;
        }




        /*
         * --------------------------------------
             REUSABLE FUNCTIONS
           --------------------------------------
        */

        // TCP process information - getting and sending the message from\to client
        static public void TCPServer()
        {

            TcpListener server = null;
            try
            {
                // Set the TcpListener  
                IPAddress ip = IPAddress.Any;

                server = new TcpListener(ip, port);
                // Console.WriteLine("I am listening for connections on " +
                //                                         IPAddress.Parse(((IPEndPoint)server.LocalEndpoint).Address.ToString()) +
                //                                          "on port number " + ((IPEndPoint)server.LocalEndpoint).Port.ToString());

                // Start listening for client requests.
                server.Start();

                // Buffer for reading data
                Byte[] bytes = new Byte[64]; // 64 symbols
                String data = null;

                // Enter the listening loop.
                while (true)
                {
                    try
                    {
                        TcpClient client = server.AcceptTcpClient();
                        if (client.Connected)
                        {

                            Task.Run(() =>
                            {  // question is Task or Parallel -> what is really works in parallel

                                // Getting the IP adress of the client
                                var localEndPoint = client.Client.LocalEndPoint as IPEndPoint;
                                var localAddress = localEndPoint.Address;
                                var localPort = localEndPoint.Port;

                                var clientIPAddress = client.Client.RemoteEndPoint;
                                //Console.WriteLine("connected to adress - " + localAddress + "; port - " + localPort + "; Remote IP address -" + clientIPAddress);

                                // Get a stream object for reading and writing
                                NetworkStream stream = client.GetStream();

                                // Loop to receive all the data sent by the client.
                                data = null;
                                int i;
                                while ((i = stream.Read(bytes, 0, bytes.Length)) != 0)
                                {
                                    // Translate data bytes to a ASCII string.
                                    data = System.Text.Encoding.ASCII.GetString(bytes, 0, i);
                                    //Console.WriteLine("Received: {0}", data);

                                    // transfer message to an array with ; split
                                    String[] separator = { ";" };
                                    String[] dataSeparator = data.Split(separator, StringSplitOptions.RemoveEmptyEntries);

                                    // Process the recieved data
                                    string dataToSend = TCPMessageProcess(dataSeparator);

                                    // Process the data sent by the client.
                                    byte[] msg = System.Text.Encoding.ASCII.GetBytes(dataToSend);

                                    // Send back a response.
                                    stream.Write(msg, 0, msg.Length);
                                    // Console.WriteLine("Sent: {0}", dataToSend);
                                }

                                // Shutdown and end connection
                                client.Close();
                            });
                        }


                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("TCP thread connection error: ", e);
                    }

                }
            }
            catch (SocketException e)
            {
                Console.WriteLine("SocketException: {0}", e);
            }
            finally
            {
                // Stop listening for new clients.
                server.Stop();
            }
        }


        // Request to DB to recieve value from SINGLE and MULTIPLE column (SELECT QUERY)
        static private List<string>[] RequestToGetValueFromDB(string queryString, string[] readerValueType, string[,] queryParameters)
        {
            List<string>[] queryResult = new List<string>[readerValueType.Length];

            for (int i = 0; i < readerValueType.Length; i++)
            {
                queryResult[i] = new List<string>();
            }

            using var connectionToDB = new SQLiteConnection(connectionToDBString);
            connectionToDB.Open();
            using var cmd = new SQLiteCommand(queryString, connectionToDB);

            for (int i = 0; i < queryParameters.Length / 2; i++)
            {
                cmd.Parameters.AddWithValue("@" + queryParameters[i, 0], queryParameters[i, 1]);
            }
            try
            {
                using SQLiteDataReader reader = cmd.ExecuteReader();

                if (reader.HasRows)
                {
                    while (reader.Read())
                    {
                        for (int i = 0; i < readerValueType.Length; i++)
                        {
                            //transfer all to string and then figure out what is what after returning the value after function
                            if (readerValueType[i] == "string") {
                                queryResult[i].Add(reader.GetString(i));
                            }
                            else if (readerValueType[i] == "int")
                            {
                                queryResult[i].Add(Convert.ToString(reader.GetInt32(i)));
                            }
                        }
                    }
                }
                else
                {
                    queryResult[0].Add(null); // return null value ? maybe fill all answer with null value in future????
                    Console.WriteLine("RequestToGetValueFromDB - No rows found.");
                }
                reader.Close();
            }
            catch (InvalidCastException e)
            {
                Console.WriteLine("error with recieveing information from login table RequestToGetValueFromDB" + e);
            }
            connectionToDB.Close();

            return queryResult;
        }


        // Authotorise player through cache
        static private bool CacheAuthorization(string playerId, string sessionToken)
        {
            bool answer = false;

            // Check if player exist in cache 
            if (playerCache.ContainsKey(playerId))
            {
                // Console.WriteLine("Player exist in session cache");

                // check if player legit
                if (playerCache[playerId][0] == sessionToken)
                {
                    // Console.WriteLine("Player Authorized");
                    answer = true;
                }
                else
                {
                    Console.WriteLine("Player is not Authorized");
                }
            }
            else
            {
                Console.WriteLine("Player does not exist in session cache");
            }

            return answer;
        }


        // GarageMainInformation - > BigSlotProcess subfunction
        //static private int[] GarageMainInformationBigSlotProcess(int bigSlotNumber, int bigSlot, string accountShipId)
        //{
        //    int shieldId = 0;
        //    int weaponControlId = 0;
        //    int bigSlotType = 0;

        //    // AccountShip.BigSlot1 = BigSlot[0];
        //    string queryString = @"SELECT BigSlot.ShieldId, BigSlot.WeaponControlId
        //                            FROM AccountShip, AccountItem, BigSlot
        //                            WHERE AccountShip.AccountShipId = @accountShipId
        //                            and AccountShip.BigSlot" + bigSlotNumber + @" = AccountItem.AccountItemId  
        //                            and AccountItem.BigSlotId = BigSlot.BigSlotId";
        //    string[,] queryParameters = new string[,] { { "accountShipId", accountShipId } };
        //    string[] stringType = new string[] { "int", "int" };
        //    List<string>[] requestAnswer = RequestToGetValueFromDB(queryString, stringType, queryParameters);

        //    shieldId = Convert.ToInt32(requestAnswer[0][0]);
        //    weaponControlId = Convert.ToInt32(requestAnswer[1][0]);

        //    if (shieldId > 0)
        //    {
        //        bigSlotType = 1;

        //        queryString = @"SELECT Shield.ShieldId
        //                            FROM Shield
        //                            WHERE Shield.ShieldId = @shieldId";
        //        queryParameters = new string[,] { { "shieldId", Convert.ToString(shieldId) } };
        //        stringType = new string[] { "int" };
        //        requestAnswer = RequestToGetValueFromDB(queryString, stringType, queryParameters);
        //        bigSlot = Convert.ToInt32(requestAnswer[0][0]);
        //    }
        //    else if (weaponControlId > 0)
        //    {
        //        bigSlotType = 2;

        //        queryString = @"SELECT WeaponContol.WeaponControlId
        //                            FROM WeaponContol
        //                            WHERE WeaponContol.WeaponControlId = @weaponControlId";
        //        queryParameters = new string[,] { { "weaponControlId", Convert.ToString(weaponControlId) } };
        //        stringType = new string[] { "int" };
        //        requestAnswer = RequestToGetValueFromDB(queryString, stringType, queryParameters);
        //        bigSlot = Convert.ToInt32(requestAnswer[0][0]);
        //    }

        //    int[] answer = new int[] { bigSlotType, bigSlot };
        //    return answer;
        //}



        // Other functions 

        // Password hash check
        static private String PasswordHash(string password)
        {

            byte[] salt = Convert.FromBase64String("xS2EOJVGIZl5hxRDEiQO3g==");

            // generate a 128-bit salt using a secure PRNG
            //byte[] salt = new byte[128 / 8];
            //using (var rng = RandomNumberGenerator.Create())
            //{
            //    rng.GetBytes(salt);
            //}
            //Console.WriteLine($"Salt: {Convert.ToBase64String(salt)}");

            // derive a 256-bit subkey (use HMACSHA1 with 10,000 iterations)
            string hashed = Convert.ToBase64String(KeyDerivation.Pbkdf2(
                password: password,
                salt: salt,
                prf: KeyDerivationPrf.HMACSHA1,
                iterationCount: 10000,
                numBytesRequested: 256 / 8));
            //  Console.WriteLine($"Hashed: {hashed}");

            return hashed;
        }





        // Initialiation of the battle session
        static private string StartSession1v1AI(int playerID)
        {
            // Check slot of the player 
            int activeSlot = 0;
            int newBattleID = -1;

            string queryString = "SELECT GarageActiveSlot  FROM Account where AccountId = @playerID ";
            string[,] queryParameters = new string[,] { { "playerID", Convert.ToString(playerID) } };
            string[] stringType = new string[] { "int" };

            activeSlot = Convert.ToInt32(RequestToGetValueFromDB(queryString, stringType, queryParameters)[0][0]);


            //-----------------------------

            using var connectionToDB = new SQLiteConnection(connectionToDBString);
            connectionToDB.Open();


            // Create Session with SesionID, playerID, playerSlot
            // add information to the DB
            string enqueryUpdate = "INSERT INTO Battle1v1Ai (AccountId, AiId, Active)  VALUES (@playerID, 0, 1);SELECT last_insert_rowid()"; //  last_insert_rowid() for sqllite
            //OUTPUT INSERTED.Battle_1v1_AI_ID
            using var commandUpdate = new SQLiteCommand(enqueryUpdate, connectionToDB);

            commandUpdate.Parameters.AddWithValue("@playerID", playerID);

            try
            {
                //var id_battle = (int)commandUpdate.ExecuteNonQuery();
                // commandUpdate.ExecuteNonQuery();
                newBattleID = Convert.ToInt32(commandUpdate.ExecuteScalar());
                //commandUpdate.ExecuteScalar();
                // Console.WriteLine("id_battle - " + newBattleID);
            }
            catch (InvalidCastException e)
            {
                Console.WriteLine("error updating DB for creating new session for 1v1 A - I" + e);
            }
            connectionToDB.Close();

            //----------------------------------------



            // Start class with battle parameters
            sessionsBattle1v1AI.Add(newBattleID, new Battle1v1AI());
            Console.WriteLine("new session with number - " + newBattleID + " is started with player -  " + sessionsBattle1v1AI[newBattleID].playerID);

            try
            {
                Console.WriteLine("try - ready");
                //  Load information to the class about the battle that going to start
                //  -----------------------------------------------------
                // LOAD INFORMATION ABOUT PLAYER TO CLASS
                sessionsBattle1v1AI[newBattleID].playerID = playerID;

                Session1v1AILoadPlayer(Convert.ToString(playerID), newBattleID);

                Console.WriteLine("Session1v1AILoadPlayer - ready");

                // LOAD INFORMATION ABOUT AI TO CLASS
                int aiId = 1;     // CHANGE IT WHEN IT WILL BE AN CHOISE FROM PLAYER TO PLAY AGAINST WHAT AI

                sessionsBattle1v1AI[newBattleID].aiId = aiId;

                Session1v1AILoadAI(Convert.ToString(aiId), newBattleID);

                Console.WriteLine("Session1v1AILoadAI - ready");

                sessionsBattle1v1AI[newBattleID].toStart = 1;
                Console.WriteLine("SESSION TO START - YES - " + sessionsBattle1v1AI[newBattleID].toStart);
            }
            catch
            {
                Console.WriteLine("Unable to create fill up battle session");
            }

            //  -----------------------------------------------------


            return Convert.ToString(newBattleID);
        }




        // get information about the modules \ ships \ crew \ weapons for battle tp the class

        static private void Session1v1AILoadAI(string aiId, int newBattleID)
        {
            // get information about ID of all modules and ship
            string queryString = @"SELECT AiShip.AiShipId, AiShip.ShipId, AiShip.EngineSlot, AiShip.CockpitSlot, AiShip.BigSlot1, AiShip.BigSlot2,
                                     AiShip.BigSlot3, AiShip.BigSlot4, AiShip.BigSlot5, AiShip.MediumSlot1, AiShip.MediumSlot2,
                                     AiShip.MediumSlot3, AiShip.MediumSlot4, AiShip.MediumSlot5, AiShip.SmallSlot1, AiShip.SmallSlot2,
                                     AiShip.SmallSlot3, AiShip.SmallSlot4, AiShip.SmallSlot5,
                                     AiShip.Weapon1, AiShip.Weapon2, AiShip.Weapon3, AiShip.Weapon4, AiShip.Weapon5
                            FROM Ai, AiShip
							WHERE 
							Ai.AiId = @aiId
                            and Ai.AiShipId = AiShip.AiShipId";
            string[,] queryParameters = new string[,] { { "aiId", aiId } };
            string[] stringType = new string[] { "int", "int", "int", "int", "int", "int", "int", "int", "int", "int", "int", "int", "int", "int", "int", "int", "int", "int", "int", "int", "int", "int", "int", "int" };
            List<string>[] requestAnswer = RequestToGetValueFromDB(queryString, stringType, queryParameters);

            string aiShipId = requestAnswer[0][0];
            string shipId = requestAnswer[1][0];
            string engineSlotId = requestAnswer[2][0];
            string cockpitSlotId = requestAnswer[3][0];
            string[] bigSlotId = new string[] { requestAnswer[4][0], requestAnswer[5][0], requestAnswer[6][0], requestAnswer[7][0], requestAnswer[8][0] };
            string[] middleSlotId = new string[] { requestAnswer[9][0], requestAnswer[10][0], requestAnswer[11][0], requestAnswer[12][0], requestAnswer[13][0] };
            string[] smallSlotId = new string[] { requestAnswer[14][0], requestAnswer[15][0], requestAnswer[16][0], requestAnswer[17][0], requestAnswer[18][0] };

            string[] weaponSlotId = new string[] { requestAnswer[19][0], requestAnswer[20][0], requestAnswer[21][0], requestAnswer[22][0], requestAnswer[23][0] };

            // get information about the ship info
            queryString = @"SELECT Ship.BaseHealth, Ship.BaseEnergy
                             FROM Ship
                             WHERE Ship.ShipId = @shipId";
            queryParameters = new string[,] { { "shipId", shipId } };
            stringType = new string[] { "int", "int" };
            requestAnswer = RequestToGetValueFromDB(queryString, stringType, queryParameters);

            sessionsBattle1v1AI[newBattleID].aiShipId = Convert.ToInt32(shipId);
            sessionsBattle1v1AI[newBattleID].aIShipMaxHealth = Convert.ToInt32(requestAnswer[0][0]);
            sessionsBattle1v1AI[newBattleID].aIShipMaxEnergy = Convert.ToInt32(requestAnswer[1][0]);
            sessionsBattle1v1AI[newBattleID].aIShipFreeEnergy = Convert.ToInt32(requestAnswer[1][0]);


            // get information about the engine slot
            if (engineSlotId != "-1" && engineSlotId != "0")
            {
                queryString = @"SELECT Engine.Health, Engine.Energy
                            FROM Engine
                             WHERE Engine.EngineId = @engineId";
                queryParameters = new string[,] { { "engineId", engineSlotId } };
                stringType = new string[] { "int", "int" };
                requestAnswer = RequestToGetValueFromDB(queryString, stringType, queryParameters);

                sessionsBattle1v1AI[newBattleID].aISlotExist[0] = 1;
                sessionsBattle1v1AI[newBattleID].aISlotHealth[0] = Convert.ToInt32(requestAnswer[0][0]);
                sessionsBattle1v1AI[newBattleID].aISlotPowered[0] = 0;
                sessionsBattle1v1AI[newBattleID].aISlotEnergyRequired[0] = Convert.ToInt32(requestAnswer[1][0]);
                sessionsBattle1v1AI[newBattleID].aISlotType[0] = "engine";
            }
            else
            {
                sessionsBattle1v1AI[newBattleID].aISlotExist[0] = 0;
            }


            // get information about the cockpit slot
            if (cockpitSlotId != "-1" && cockpitSlotId != "0")
            {
                queryString = @"SELECT Cockpit.Health, Cockpit.Energy
                            FROM Cockpit
                             WHERE Cockpit.EngineId = @cockpitId";
                queryParameters = new string[,] { { "cockpitId", cockpitSlotId } };
                stringType = new string[] { "int", "int" };
                requestAnswer = RequestToGetValueFromDB(queryString, stringType, queryParameters);

                sessionsBattle1v1AI[newBattleID].aISlotExist[1] = 1;
                sessionsBattle1v1AI[newBattleID].aISlotHealth[1] = Convert.ToInt32(requestAnswer[0][0]);
                sessionsBattle1v1AI[newBattleID].aISlotPowered[1] = 0;
                sessionsBattle1v1AI[newBattleID].aISlotEnergyRequired[1] = Convert.ToInt32(requestAnswer[1][0]);
                sessionsBattle1v1AI[newBattleID].aISlotType[1] = "cockpit";
            }
            else
            {
                sessionsBattle1v1AI[newBattleID].aISlotExist[1] = 0;
            }



            // get information about the bigslots
            for (int i = 0; i < bigSlotId.Length; i++)
            {
                if (bigSlotId[i] != "-1" && bigSlotId[i] != "0")
                {
                    queryString = @"SELECT BigSlot.ShieldId, BigSlot.WeaponControlId
                                FROM BigSlot
                                 WHERE BigSlot.BigSlotId = @bigSlotId";
                    queryParameters = new string[,] { { "bigSlotId", bigSlotId[i] } };
                    stringType = new string[] { "int", "int" };
                    requestAnswer = RequestToGetValueFromDB(queryString, stringType, queryParameters);

                    sessionsBattle1v1AI[newBattleID].aISlotExist[i + 2] = 1;

                    //get information if shield or weaponcontrol
                    if (requestAnswer[0][0] != "0") //shield
                    {
                        queryString = @"SELECT Shield.Heath, Shield.Energy, Shield.Capacity, Shield.RechargeRate, Shield.RechargeTime
                                        FROM Shield
                                        WHERE Shield.ShieldId = @shieldId";
                        queryParameters = new string[,] { { "shieldId", requestAnswer[0][0] } };
                        stringType = new string[] { "int", "int", "int", "int", "int" };
                        requestAnswer = RequestToGetValueFromDB(queryString, stringType, queryParameters);

                        sessionsBattle1v1AI[newBattleID].aISlotHealth[i + 2] = Convert.ToInt32(requestAnswer[0][0]);
                        sessionsBattle1v1AI[newBattleID].aISlotPowered[i + 2] = 0;
                        sessionsBattle1v1AI[newBattleID].aISlotEnergyRequired[i + 2] = Convert.ToInt32(requestAnswer[1][0]);
                        sessionsBattle1v1AI[newBattleID].aISlotType[i + 2] = "shield";

                        sessionsBattle1v1AI[newBattleID].aISlotShieldCapacity[i + 2] = Convert.ToInt32(requestAnswer[2][0]);
                        sessionsBattle1v1AI[newBattleID].aISlotShieldRechargeTime[i + 2] = Convert.ToInt32(requestAnswer[3][0]);
                        sessionsBattle1v1AI[newBattleID].aISlotShieldRechargeCurrentTime[i + 2] = 0;
                        sessionsBattle1v1AI[newBattleID].aISlotShieldRechargeRate[i + 2] = Convert.ToInt32(requestAnswer[4][0]);


                    }
                    else if (requestAnswer[1][0] != "0") // weapon control
                    {
                        queryString = @"SELECT WeaponContol.Health, WeaponContol.Energy, WeaponContol.AmountOfWeapons
                        FROM WeaponContol
                        WHERE WeaponContol.WeaponControlId = @weaponControlId";
                        queryParameters = new string[,] { { "weaponControlId", requestAnswer[1][0] } };
                        stringType = new string[] { "int", "int", "int" };
                        requestAnswer = RequestToGetValueFromDB(queryString, stringType, queryParameters);

                        sessionsBattle1v1AI[newBattleID].aISlotHealth[0] = Convert.ToInt32(requestAnswer[0][0]);
                        sessionsBattle1v1AI[newBattleID].aISlotPowered[0] = 0;
                        sessionsBattle1v1AI[newBattleID].aISlotEnergyRequired[0] = Convert.ToInt32(requestAnswer[1][0]);
                        sessionsBattle1v1AI[newBattleID].aISlotType[0] = "weaponcontrol";
                        sessionsBattle1v1AI[newBattleID].aISlotWeaponControlAmountOfWeapons[0] = Convert.ToInt32(requestAnswer[2][0]);
                    }
                }
                else
                {
                    sessionsBattle1v1AI[newBattleID].aISlotExist[i + 2] = 0;
                }
            }

            // get information about the middleslots
            for (int i = 0; i < middleSlotId.Length; i++)
            {
                if (middleSlotId[i] != "-1" && middleSlotId[i] != "0")
                {
                    sessionsBattle1v1AI[newBattleID].aISlotExist[i + 7] = 1;
                }
                else
                {
                    sessionsBattle1v1AI[newBattleID].aISlotExist[i + 7] = 0;
                }
            }

            // get information about the weapons

            for (int i = 0; i < weaponSlotId.Length; i++)
            {
                if (weaponSlotId[i] != "-1" && weaponSlotId[i] != "0")
                {
                    sessionsBattle1v1AI[newBattleID].aIWeaponSlotExist[i] = 1;

                    queryString = @"SELECT Weapon.Energy, Weapon.Damage, Weapon.ReloadTime
                                    FROM Weapon
                                    WHERE Weapon.WeaponId = @weaponId";
                    queryParameters = new string[,] { { "weaponId", weaponSlotId[i] } };
                    stringType = new string[] { "int", "int", "int" };
                    requestAnswer = RequestToGetValueFromDB(queryString, stringType, queryParameters);

                    sessionsBattle1v1AI[newBattleID].aIWeaponSlotPowered[i] = 1;
                    sessionsBattle1v1AI[newBattleID].aIWeaponSlotEnergyRequired[i] = Convert.ToInt32(requestAnswer[0][0]);
                    sessionsBattle1v1AI[newBattleID].aIWeaponSlotDamage[i] = Convert.ToInt32(requestAnswer[1][0]);
                    sessionsBattle1v1AI[newBattleID].aIWeaponSlotReloadTime[i] = Convert.ToInt32(requestAnswer[2][0]);
                }
                else
                {
                    sessionsBattle1v1AI[newBattleID].aIWeaponSlotExist[i] = 0;
                }
            }



            // Crew // TO CORRECT
            sessionsBattle1v1AI[newBattleID].aICrewExist[0] = 1;
            sessionsBattle1v1AI[newBattleID].aICrewHealth[0] = 10;
            sessionsBattle1v1AI[newBattleID].aICrewDamage[0] = 1;

        }

        static private void Session1v1AILoadPlayer(string accountId, int newBattleID)
        {
            // get information about ID of all modules and ship
            string queryString = @"SELECT AccountShip.AccountShipId, AccountShip.ShipId, AccountShip.EngineSlot, AccountShip.CockpitSlot, AccountShip.BigSlot1, AccountShip.BigSlot2,
                                     AccountShip.BigSlot3, AccountShip.BigSlot4, AccountShip.BigSlot5, AccountShip.MediumSlot1, AccountShip.MediumSlot2,
                                     AccountShip.MediumSlot3, AccountShip.MediumSlot4, AccountShip.MediumSlot5, AccountShip.SmallSlot1, AccountShip.SmallSlot2,
                                     AccountShip.SmallSlot3, AccountShip.SmallSlot4, AccountShip.SmallSlot5,
                                     AccountShip.Weapon1, AccountShip.Weapon2, AccountShip.Weapon3, AccountShip.Weapon4, AccountShip.Weapon5
                            FROM AccountShip, Garage, Account
							WHERE 
							Account.AccountId = @accountId
                            and Garage.Slot = Account.GarageActiveSlot 
                            and Garage.AccountId = Account.AccountId
                            and AccountShip.AccountShipId = Garage.AccountShipId";
            string[,]  queryParameters = new string[,] { { "accountShipId", accountId } };
            string[]  stringType = new string[] { "int", "int", "int", "int", "int", "int", "int", "int", "int", "int", "int", "int", "int", "int", "int", "int", "int", "int", "int", "int", "int", "int", "int", "int" };
            List<string>[] requestAnswer = RequestToGetValueFromDB(queryString, stringType, queryParameters);

            string accountShipId = requestAnswer[0][0];
            string shipId = requestAnswer[1][0];
            string engineSlotId = requestAnswer[2][0];
            string cockpitSlotId = requestAnswer[3][0];
            string[] bigSlotId = new string[] { requestAnswer[4][0] , requestAnswer[5][0] , requestAnswer[6][0] , requestAnswer[7][0] , requestAnswer[8][0] };
            string[] middleSlotId = new string[] { requestAnswer[9][0], requestAnswer[10][0], requestAnswer[11][0], requestAnswer[12][0], requestAnswer[13][0] };
            string[] smallSlotId = new string[] { requestAnswer[14][0], requestAnswer[15][0], requestAnswer[16][0], requestAnswer[17][0], requestAnswer[18][0] };

            string[] weaponSlotId = new string[] { requestAnswer[19][0], requestAnswer[20][0], requestAnswer[21][0], requestAnswer[22][0], requestAnswer[23][0] };

            // get information about the ship info
            queryString = @"SELECT Ship.BaseHealth, Ship.BaseEnergy
                             FROM Ship
                             WHERE Ship.ShipId = @shipId";
            queryParameters = new string[,] { { "shipId", shipId } };
            stringType = new string[] { "int", "int"  };
            requestAnswer = RequestToGetValueFromDB(queryString, stringType, queryParameters);

            sessionsBattle1v1AI[newBattleID].playerShipId = Convert.ToInt32(shipId);
            sessionsBattle1v1AI[newBattleID].playerShipMaxHealth = Convert.ToInt32(requestAnswer[0][0]);
            sessionsBattle1v1AI[newBattleID].playerShipMaxEnergy = Convert.ToInt32(requestAnswer[1][0]);
            sessionsBattle1v1AI[newBattleID].playerShipFreeEnergy = Convert.ToInt32(requestAnswer[1][0]);


            // get information about the engine slot
            if (engineSlotId != "-1" && engineSlotId != "0")
            {
                queryString = @"SELECT Engine.Health, Engine.Energy
                            FROM Engine
                             WHERE Engine.EngineId = @engineId";
                queryParameters = new string[,] { { "engineId", engineSlotId } };
                stringType = new string[] { "int", "int" };
                requestAnswer = RequestToGetValueFromDB(queryString, stringType, queryParameters);

                sessionsBattle1v1AI[newBattleID].playerSlotExist[0] = 1;
                sessionsBattle1v1AI[newBattleID].playerSlotHealth[0] = Convert.ToInt32(requestAnswer[0][0]);
                sessionsBattle1v1AI[newBattleID].playerSlotPowered[0] = 0;
                sessionsBattle1v1AI[newBattleID].playerSlotEnergyRequired[0] = Convert.ToInt32(requestAnswer[1][0]);
                sessionsBattle1v1AI[newBattleID].playerSlotType[0] = "engine";
            }
            else
            {
                sessionsBattle1v1AI[newBattleID].playerSlotExist[0] = 0;
            }


            // get information about the cockpit slot
            if (cockpitSlotId != "-1" && cockpitSlotId != "0")
            {
                queryString = @"SELECT Cockpit.Health, Cockpit.Energy
                            FROM Cockpit
                             WHERE Cockpit.EngineId = @cockpitId";
                queryParameters = new string[,] { { "cockpitId", cockpitSlotId } };
                stringType = new string[] { "int", "int" };
                requestAnswer = RequestToGetValueFromDB(queryString, stringType, queryParameters);

                sessionsBattle1v1AI[newBattleID].playerSlotExist[1] = 1;
                sessionsBattle1v1AI[newBattleID].playerSlotHealth[1] = Convert.ToInt32(requestAnswer[0][0]);
                sessionsBattle1v1AI[newBattleID].playerSlotPowered[1] = 0;
                sessionsBattle1v1AI[newBattleID].playerSlotEnergyRequired[1] = Convert.ToInt32(requestAnswer[1][0]);
                sessionsBattle1v1AI[newBattleID].playerSlotType[1] = "cockpit";
            }
            else
            {
                sessionsBattle1v1AI[newBattleID].playerSlotExist[1] = 0;
            }



            // get information about the bigslots
            for (int i = 0; i < bigSlotId.Length; i++)
            {
                if (bigSlotId[i] != "-1" && bigSlotId[i] != "0") 
                {
                    queryString = @"SELECT BigSlot.ShieldId, BigSlot.WeaponControlId
                                FROM BigSlot
                                 WHERE BigSlot.BigSlotId = @bigSlotId";
                    queryParameters = new string[,] { { "bigSlotId", bigSlotId[i] } };
                    stringType = new string[] { "int", "int" };
                    requestAnswer = RequestToGetValueFromDB(queryString, stringType, queryParameters);

                    sessionsBattle1v1AI[newBattleID].playerSlotExist[i+2] = 1;

                    //get information if shield or weaponcontrol
                    if (requestAnswer[0][0] != "0") //shield
                    {
                        queryString = @"SELECT Shield.Heath, Shield.Energy, Shield.Capacity, Shield.RechargeRate, Shield.RechargeTime
                                        FROM Shield
                                        WHERE Shield.ShieldId = @shieldId";
                        queryParameters = new string[,] { { "shieldId", requestAnswer[0][0] } };
                        stringType = new string[] { "int", "int", "int", "int", "int" };
                        requestAnswer = RequestToGetValueFromDB(queryString, stringType, queryParameters);

                        sessionsBattle1v1AI[newBattleID].playerSlotHealth[i + 2] = Convert.ToInt32(requestAnswer[0][0]);
                        sessionsBattle1v1AI[newBattleID].playerSlotPowered[i + 2] = 0;
                        sessionsBattle1v1AI[newBattleID].playerSlotEnergyRequired[i + 2] = Convert.ToInt32(requestAnswer[1][0]);
                        sessionsBattle1v1AI[newBattleID].playerSlotType[i + 2] = "shield";

                        sessionsBattle1v1AI[newBattleID].playerSlotShieldCapacity[i + 2] = Convert.ToInt32(requestAnswer[2][0]);
                        sessionsBattle1v1AI[newBattleID].playerSlotShieldRechargeTime[i + 2] = Convert.ToInt32(requestAnswer[3][0]);
                        sessionsBattle1v1AI[newBattleID].playerSlotShieldRechargeCurrentTime[i + 2] = 0;
                        sessionsBattle1v1AI[newBattleID].playerSlotShieldRechargeRate[i + 2] = Convert.ToInt32(requestAnswer[4][0]);


                    }
                    else if (requestAnswer[1][0] != "0") // weapon control
                    {
                        queryString = @"SELECT WeaponContol.Health, WeaponContol.Energy, WeaponContol.AmountOfWeapons
                        FROM WeaponContol
                        WHERE WeaponContol.WeaponControlId = @weaponControlId";
                        queryParameters = new string[,] { { "weaponControlId", requestAnswer[1][0] } };
                        stringType = new string[] { "int", "int", "int" };
                        requestAnswer = RequestToGetValueFromDB(queryString, stringType, queryParameters);
                        
                        sessionsBattle1v1AI[newBattleID].playerSlotHealth[0] = Convert.ToInt32(requestAnswer[0][0]);
                        sessionsBattle1v1AI[newBattleID].playerSlotPowered[0] = 0;
                        sessionsBattle1v1AI[newBattleID].playerSlotEnergyRequired[0] = Convert.ToInt32(requestAnswer[1][0]);
                        sessionsBattle1v1AI[newBattleID].playerSlotType[0] = "weaponcontrol";
                        sessionsBattle1v1AI[newBattleID].playerSlotWeaponControlAmountOfWeapons[0] = Convert.ToInt32(requestAnswer[2][0]);
                    }
                }
                else
                {
                    sessionsBattle1v1AI[newBattleID].playerSlotExist[i + 2] = 0;
                }
            }

            // get information about the middleslots
            for (int i = 0; i < middleSlotId.Length; i++)
            {
                if (middleSlotId[i] != "-1" && middleSlotId[i] != "0") 
                {
                    sessionsBattle1v1AI[newBattleID].playerSlotExist[i + 7] = 1;
                }
                else
                {
                    sessionsBattle1v1AI[newBattleID].playerSlotExist[i + 7] = 0;
                }
            }

            // get information about the weapons

            for (int i = 0; i < weaponSlotId.Length; i++)
            {
                if (weaponSlotId[i] != "-1" && weaponSlotId[i] != "0") 
                {
                    sessionsBattle1v1AI[newBattleID].playerWeaponSlotExist[i] = 1;

                    queryString = @"SELECT Weapon.Energy, Weapon.Damage, Weapon.ReloadTime
                                    FROM Weapon
                                    WHERE Weapon.WeaponId = @weaponId";
                    queryParameters = new string[,] { { "weaponId", weaponSlotId[i] } };
                    stringType = new string[] { "int", "int", "int" };
                    requestAnswer = RequestToGetValueFromDB(queryString, stringType, queryParameters);

                    sessionsBattle1v1AI[newBattleID].playerWeaponSlotPowered[i] = 1;
                    sessionsBattle1v1AI[newBattleID].playerWeaponSlotEnergyRequired[i] = Convert.ToInt32(requestAnswer[0][0]);
                    sessionsBattle1v1AI[newBattleID].playerWeaponSlotDamage[i] = Convert.ToInt32(requestAnswer[1][0]);
                    sessionsBattle1v1AI[newBattleID].playerWeaponSlotReloadTime[i] = Convert.ToInt32(requestAnswer[2][0]);
                }
                else
                {
                    sessionsBattle1v1AI[newBattleID].playerWeaponSlotExist[i] = 0;
                }
            }



            // Crew // TO CORRECT
            sessionsBattle1v1AI[newBattleID].playerCrewExist[0] = 1;
            sessionsBattle1v1AI[newBattleID].playerCrewHealth[0] = 10;
            sessionsBattle1v1AI[newBattleID].playerCrewDamage[0] = 1;

        }
    }







    /*
      Battle1v1AI - class for battle, contain all information about the battle
        and all actions that can be in that battle
    */
    public class Battle1v1AI {

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

        //Variables

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

        public int aiShipId { get; set; } 


        ////-------------------- Player -------------------------

        //// Basic ship parameters
        //public int playerHealthMax { get; set; }
        //public int playerHealthCurrent { get; set; }
        //public int playerEnergyMax { get; set; }
        //public int playerEnergyFree { get; set; }

        //// Weapon Control
        //public int playerWeaponControlHealthMax { get; set; }
        //public int playerWeaponControlHealthCurrent { get; set; }
        //public int playerWeaponControlEnergyRequired { get; set; }
        //public int playerWeaponControlEnergyCurrent { get; set; }

        //// Weapon 1 
        //public string playerWeapon1Name { get; set; }
        //public int playerWeapon1Damage { get; set; }
        //public int playerWeapon1ReloadTime { get; set; }
        //public int playerWeapon1ReloadCurrent { get; set; }
        //public int playerWeapon1EnergyRequired { get; set; }
        //public int playerWeapon1EnergyCurrent { get; set; }




        ////-------------------- AI -------------------------
        //// Basic ship parameters
        //public int aiHealthMax { get; set; }
        //public int aiHealthCurrent { get; set; }
        //public int aiEnergyMax { get; set; }
        //public int aiEnergyFree { get; set; }

        //// Weapon Control
        //public int aiWeaponControlHealthMax { get; set; }
        //public int aiWeaponControlHealthCurrent { get; set; }
        //public int aiWeaponControlEnergyRequired { get; set; }
        //public int aiWeaponControlEnergyCurrent { get; set; }

        //// Weapon 1 
        //public int aiWeapon1Damage { get; set; }
        //public int aiWeapon1ReloadTime { get; set; }
        //public int aiWeapon1ReloadCurrent { get; set; }
        //public int aiWeapon1EnergyRequired { get; set; }
        //public int aiWeapon1EnergyCurrent { get; set; }



        // NEW SYSTEM

            // PLAYER

        // ship

        public int playerShipMaxHealth { get; set; }
        public int playerShipCurrentHealth { get; set; }
        public int playerShipMaxEnergy { get; set; }
        public int playerShipFreeEnergy { get; set; }

        // p modules

        //[ engine , cockpit, biglot1 .. 5, mediumslot 1 .. 5]

        public int[] playerSlotExist { get; set; }
        public int[] playerSlotHealth { get; set; }
        public int[] playerSlotPowered { get; set; }
        public int[] playerSlotEnergyRequired { get; set; }
        public string[] playerSlotType { get; set; }

        //bigslot difference slots
        public int[] playerSlotShieldCapacity { get; set; }    
        public int[] playerSlotShieldRechargeTime { get; set; }    
        public int[] playerSlotShieldRechargeCurrentTime { get; set; }    
        public int[] playerSlotShieldRechargeRate  { get; set; }    

        public int[] playerSlotWeaponControlAmountOfWeapons { get; set; } 

        // weapons

        public int[] playerWeaponSlotExist { get; set; }
        public int[] playerWeaponSlotPowered { get; set; }
        public int[] playerWeaponSlotEnergyRequired { get; set; }
        public int[] playerWeaponSlotDamage { get; set; }
        public int[] playerWeaponSlotReloadTime { get; set; }
        public int[] playerWeaponSlotCurrentReloadTime { get; set; }

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

        public int[] aISlotExist { get; set; }
        public int[] aISlotHealth { get; set; }
        public int[] aISlotPowered { get; set; }
        public int[] aISlotEnergyRequired { get; set; }
        public string[] aISlotType { get; set; }

        //bigslot difference slots
        public int[] aISlotShieldCapacity { get; set; }
        public int[] aISlotShieldRechargeTime { get; set; }
        public int[] aISlotShieldRechargeCurrentTime { get; set; }
        public int[] aISlotShieldRechargeRate { get; set; }

        public int[] aISlotWeaponControlAmountOfWeapons { get; set; }

        // weapons

        public int[] aIWeaponSlotExist { get; set; }
        public int[] aIWeaponSlotPowered { get; set; }
        public int[] aIWeaponSlotEnergyRequired { get; set; }
        public int[] aIWeaponSlotDamage { get; set; }
        public int[] aIWeaponSlotReloadTime { get; set; }
        public int[] aIWeaponSlotCurrentReloadTime { get; set; }

        // Crew

        public int[] aICrewExist { get; set; }
        public int[] aICrewHealth { get; set; }
        public int[] aICrewDamage { get; set; }


    }

}
