﻿using Server.Service.Handlers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Server.DataContracts;

namespace Server {
    /// <summary>
    /// Here the server parse the received message and initializes the appropriate handler
    /// </summary>
    class ServerHandleData {
        public delegate void PacketF(int ConnectionID, byte[] Data);    //Signature of functions of type (int, byte[])
        public static Dictionary<int, PacketF> PacketListener;          //Maps functionID -> function; read-only hence thread-safe

        /// <summary>
        /// Maps integers to corresponding functions
        /// </summary>
        public static void InitializePacketListener() {
            PacketListener = new Dictionary<int, PacketF> {
                { (int)ClientPackages.CMsg, HandleMessage },
                { (int)ClientPackages.CLogin, HandleLogin },
                { (int)ClientPackages.CRegister, HandleRegister },
                { (int)ClientPackages.CGetUserSchedule, HandleGetUserSchedule },
                { (int)ClientPackages.CCreateTeam, HandleCreateTeam }
            };
        }

        /// <summary>
        /// Makes sure a newly connected user is a valid user of our application
        /// </summary>
        /// <param name="ConnectionID"></param>
        /// <returns></returns>
        public static bool HandleAuth(int ConnectionID) {
            if (ServerTCP.ClientObjects[ConnectionID].bufferH.Length() < 12) {
                ServerTCP.ClientObjects[ConnectionID].CloseConnection();
                return false;
            }

            int len = ServerTCP.ClientObjects[ConnectionID].bufferH.ReadInteger();
            int id1 = ServerTCP.ClientObjects[ConnectionID].bufferH.ReadInteger();
            int id2 = ServerTCP.ClientObjects[ConnectionID].bufferH.ReadInteger();
            if (len == 8 && id1 == 19239485 && id2 == 5680973) {
                ServerTCP.ClientObjects[ConnectionID].authenticated = true;
                Console.WriteLine(ConnectionID + " was successfully authenticated");
                return true;
            }
            else {
                ServerTCP.ClientObjects[ConnectionID].CloseConnection();
                Console.WriteLine(ConnectionID + " is fake");
                return false;
            }
        }

        /// <summary>
        /// Makes sure that we process every byte of every packet received
        /// </summary>
        /// <param name="ConnectionID"></param>
        /// <param name="data"></param>
        /// In case of packets > 4096 bytes, HandleData will be called mutliple times
        /// and only when all the data is here (length>=plength), the loop will be entered
        public static void HandleData(int ConnectionID, byte[] data)        //Static method is fine since each thread has its own stack 
        {
            try {
                //Writing on the console what is received (for debugging)
                /*
                foreach (byte bb in data) { Console.Write(bb + " "); }
                Console.Write('\n');
                foreach (byte bb in data) { Console.Write((char)bb); }
                Console.Write('\n');
                */

                if (data == null) { Console.WriteLine("No data..."); return; }

                int pLength = 0;    //Packet length
                byte[] buffer = (byte[])data.Clone();   //To avoid shallow copies

                if (!ServerTCP.ClientObjects.ContainsKey(ConnectionID)) { return; }
                if (ServerTCP.ClientObjects[ConnectionID].bufferH == null)
                    ServerTCP.ClientObjects[ConnectionID].bufferH = new BufferHelper();

                ServerTCP.ClientObjects[ConnectionID].bufferH.WriteBytes(buffer);
                if (!ServerTCP.ClientObjects[ConnectionID].authenticated) {
                    bool a = HandleAuth(ConnectionID);
                    if (!a) { return; }
                    if (ServerTCP.ClientObjects[ConnectionID].bufferH.Length() == 0) { return; }
                }

                if (ServerTCP.ClientObjects[ConnectionID].bufferH.Length() < 4) {
                    Console.WriteLine("Buffer is too empty");
                    ServerTCP.ClientObjects[ConnectionID].bufferH.Clear();
                    return;
                }

                if (!ServerTCP.ClientObjects.ContainsKey(ConnectionID)) { return; }
                pLength = ServerTCP.ClientObjects[ConnectionID].bufferH.ReadInteger(false);  //Advances only when the whole packet is here
                while (pLength >= 4 && pLength <= ServerTCP.ClientObjects[ConnectionID].bufferH.Length() - 4)    //-4 since readPos hasn't advanced yet
                {
                    ServerTCP.ClientObjects[ConnectionID].bufferH.ReadInteger();

                    int packageID = ServerTCP.ClientObjects[ConnectionID].bufferH.ReadInteger();
                    pLength -= 4;
                    data = ServerTCP.ClientObjects[ConnectionID].bufferH.ReadBytes(pLength);

                    //Call the appropriate function in case of a correct packageID
                    if (PacketListener.TryGetValue(packageID, out PacketF packet))
                        packet.Invoke(ConnectionID, data);
                    else { Console.WriteLine("Wrong function ID"); pLength = 0; break; }

                    if (!ServerTCP.ClientObjects.ContainsKey(ConnectionID)) { return; }

                    pLength = 0;
                    if (ServerTCP.ClientObjects[ConnectionID].bufferH.Length() >= 4)
                        pLength = ServerTCP.ClientObjects[ConnectionID].bufferH.ReadInteger(false);
                }

                if (pLength < 4 && ServerTCP.ClientObjects.ContainsKey(ConnectionID)) { ServerTCP.ClientObjects[ConnectionID].bufferH.Clear(); }
            }
            catch (Exception e) {
                Console.WriteLine(e.ToString());
            }
        }


        private static void HandleMessage(int ConnectionID, byte[] data) {
            BufferHelper bufferH = new BufferHelper();
            bufferH.WriteBytes(data);
            string msg = bufferH.ReadString();
            Console.WriteLine(msg);
            bufferH.Dispose();
        }

        private static void HandleRegister(int ConnectionID, byte[] data)
        {
            BufferHelper bufferH = new BufferHelper();
            bufferH.WriteBytes(data);

            // Read username and password to buffer
            string username = bufferH.ReadString();
            string firstname = bufferH.ReadString();
            string lastName = bufferH.ReadString();
            string password = bufferH.ReadString();
            string confirmPassowrd = bufferH.ReadString();
            string email = bufferH.ReadString();
            int day = bufferH.ReadInteger();
            int month = bufferH.ReadInteger();
            int year = bufferH.ReadInteger();
            DateTime datoOfBirth = new DateTime(year, month, day);

            Console.WriteLine(username + " has joined the party!!!");

            bufferH.Dispose();

            // Call AccountsHandler 
            int isRegistered = AccountsHandler.confirmRegistration(username, firstname, lastName, email, password, confirmPassowrd, datoOfBirth);

            Console.WriteLine(isRegistered);
            // If account exists notify the front end to change scenes
            ServerTCP.PACKET_SendRegisterReply(ConnectionID, isRegistered);
        }

        //Some client used PACKAGE_Login, we handle it here
        private static void HandleLogin(int ConnectionID, byte[] data) {
            BufferHelper bufferH = new BufferHelper();
            bufferH.WriteBytes(data);

            // Read username and password to buffer
            string username = bufferH.ReadString();
            string password = bufferH.ReadString();

            bufferH.Dispose();

            // Call AccountsHandler 
            bool isUser = AccountsHandler.confirmLogIn(username, password);

            if (isUser) {
                ServerTCP.ClientObjects[ConnectionID].username = username;
                ServerTCP.UsernameToConnectionID[username] = ConnectionID;
                //TODO: set this user to be online in the db HERE (to make sure UsernameToConnectionID is set accordingly)
            }

            // If account exists notify the front end to change scenes
            ServerTCP.PACKET_SendLoginReply(ConnectionID, isUser);
        }

        private static void HandleGetUserSchedule(int ConnectionID, byte[] data)
        {
            BufferHelper bufferH = new BufferHelper();
            bufferH.WriteBytes(data);

            // Read username and password to buffer
            string userID = bufferH.ReadString();

            bufferH.Dispose();

            // Call SchedulesHandler to get list of events in the schedule
            int[] e = SchedulesHandler.getEventList(userID);
            // Call EventsHandler to get details of each event in the eventsID list
            // and add them to a string to be sent to the client
            ////////////TO FIX/////////////////////////////////
            string events="";
            foreach (int i in e)
            {
                events += i;
                string details = EventsHandler.getPersonalEvent(i);
                events+=details;
            }
            ServerTCP.PACKET_SendGetUserScheduleReply(ConnectionID, events);
        }

        private static void HandleCreateTeam(int ConnectionID, byte[] data) {
            BufferHelper bufferH = new BufferHelper();
            bufferH.WriteBytes(data);

            string teamName = bufferH.ReadString();
            string admin = bufferH.ReadString();
            int numberOfMembers = bufferH.ReadInteger();
            string[] members = new string[numberOfMembers];
            for(int i = 0; i < numberOfMembers; i++) {
                members[i] = bufferH.ReadString();
            }

            bufferH.Dispose();

            TeamsHandler.createTeamRequest(ConnectionID, admin, teamName, members);
        }

    }
}
