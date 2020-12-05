﻿using Server.DataContracts;
using Server.Service.Storage;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;

namespace AUBTimeManagementApp.Service.Storage
{
    public class InvitationsStorage {
        
        public static bool invitationExists(int eventID, int teamID, string senderUsername) {
            try {
                string connectionString = ConnectionUtil.connectionString;
                SqlConnection sqlConnection = new SqlConnection(connectionString);
                sqlConnection.Open();

                string query = "SELECT * FROM Invitations WHERE EventID = @EventID AND TeamID = @TeamID AND SenderUsername = @SenderUsername";
                SqlCommand command = new SqlCommand(query, sqlConnection);

                command.Parameters.Add("@EventID", SqlDbType.Int).Value = eventID;
                command.Parameters.Add("@TeamID", SqlDbType.Int).Value = teamID;
                command.Parameters.Add("@SenderUsername", SqlDbType.NVarChar).Value = senderUsername;
                SqlDataReader dataReader = command.ExecuteReader();

                bool res = dataReader.HasRows;
                command.Parameters.Clear(); dataReader.Close();
                sqlConnection.Close(); return res;
            }
            catch (Exception exception) { Console.WriteLine("invitationExists: " + exception.Message); throw; }
        }

        private static int getInvitationID(int eventID, int teamID, string senderUsername) {
            try {
                string connectionString = ConnectionUtil.connectionString;
                SqlConnection sqlConnection = new SqlConnection(connectionString);
                sqlConnection.Open();

                string query = "SELECT InvitationID FROM Invitations WHERE EventID = @EventID AND TeamID = @TeamID AND SenderUsername = @SenderUsername";
                SqlCommand command = new SqlCommand(query, sqlConnection);

                command.Parameters.Add("@EventID", SqlDbType.Int).Value = eventID;
                command.Parameters.Add("@TeamID", SqlDbType.Int).Value = teamID;
                command.Parameters.Add("@SenderUsername", SqlDbType.NVarChar).Value = senderUsername;
                SqlDataReader dataReader = command.ExecuteReader();

                int res = (dataReader.Read() ? dataReader.GetInt32(0) : -1);
                command.Parameters.Clear(); dataReader.Close();
                sqlConnection.Close(); return res;
            }
            catch (Exception exception) { Console.WriteLine("getInvitation: " + exception.Message); throw; }
        }

        public static int AddInvitation(int eventID, int teamID, string senderUsername) {
            try {
                if (invitationExists(eventID, teamID, senderUsername)) { return getInvitationID(eventID, teamID, senderUsername); }
                string connectionString = ConnectionUtil.connectionString;
                SqlConnection sqlConnection = new SqlConnection(connectionString);
                sqlConnection.Open();

                string query = "INSERT INTO Invitations (EventID, TeamID, SenderUsername, Pending) " +
                                "VALUES (@EventID, @TeamID, @SenderUsername, @numberOfInvitees)";
                SqlCommand command = new SqlCommand(query, sqlConnection);

                command.Parameters.Add("@EventID", SqlDbType.Int).Value = eventID;
                command.Parameters.Add("@TeamID", SqlDbType.Int).Value = teamID;
                command.Parameters.Add("@SenderUsername", SqlDbType.NVarChar).Value = senderUsername;
                command.Parameters.Add("@numberOfInvitees", SqlDbType.Int).Value = 0;
                SqlDataReader dataReader = command.ExecuteReader();

                command.Parameters.Clear(); dataReader.Close();
                sqlConnection.Close(); return getInvitationID(eventID, teamID, senderUsername);
            }
            catch (Exception exception) { Console.WriteLine("AddInvitation: " + exception.Message); throw; }
        }

        public static void AddUserInvitation(string username, int invitationID) { 
            try {
                string connectionString = ConnectionUtil.connectionString;
                SqlConnection sqlConnection = new SqlConnection(connectionString);
                sqlConnection.Open();

                string query = "INSERT INTO isInvited (Username, InvitationID) " +
                                "VALUES (@Username, @InvitationID)";
                SqlCommand command = new SqlCommand(query, sqlConnection);

                command.Parameters.Add("@Username", SqlDbType.NVarChar).Value = username;
                command.Parameters.Add("@InvitationID", SqlDbType.Int).Value = invitationID;
                SqlDataReader dataReader = command.ExecuteReader();

                command.Parameters.Clear(); dataReader.Close();

                query = "SELECT Pending FROM Invitations WHERE InvitationID = @InvitationID";
                command = new SqlCommand(query, sqlConnection);
                command.Parameters.Add("@InvitationID", SqlDbType.Int).Value = invitationID;

                dataReader = command.ExecuteReader();
                int pending = dataReader.Read() ? dataReader.GetInt32(0) : 0;
                command.Parameters.Clear(); dataReader.Close();

                query = "UPDATE Invitations SET Pending = @Pending WHERE InvitationID = @InvitationID";
                command = new SqlCommand(query, sqlConnection);
                command.Parameters.Add("@Pending", SqlDbType.Int).Value = pending + 1;
                command.Parameters.Add("@InvitationID", SqlDbType.Int).Value = invitationID;

                dataReader = command.ExecuteReader();
                command.Parameters.Clear(); dataReader.Close(); sqlConnection.Close();
            }
            catch (Exception exception) { Console.WriteLine("AddUserInvitation: " + exception.Message); throw; }
        }

        public static List<int> GetUserInvitations(string username)
        {
            try {
                string connectionString = ConnectionUtil.connectionString;
                SqlConnection sqlConnection = new SqlConnection(connectionString);
                sqlConnection.Open();

                string query = "SELECT InvitationID FROM isInvited WHERE Username = @Username";

                SqlCommand command = new SqlCommand(query, sqlConnection);
                command.Parameters.Add("@Username", SqlDbType.NVarChar).Value = username;
                SqlDataReader dataReader = command.ExecuteReader();

                List<int> invitations = new List<int>();
                while (dataReader.Read()) { invitations.Add(dataReader.GetInt32(0)); }
                command.Parameters.Clear(); dataReader.Close();
                sqlConnection.Close(); return invitations;
            }
            catch (Exception exception) { Console.WriteLine("GetUserInvitations: " + exception.Message); throw; }
        }

        public static List<Invitation> GetInvitations(List<int> invitationIDs) {

            if (invitationIDs.Count == 0) { return new List<Invitation>(); }
            try {
                string connectionString = ConnectionUtil.connectionString;
                SqlConnection sqlConnection = new SqlConnection(connectionString);
                sqlConnection.Open();

                string combinedStringInvitationIDs = string.Join(",", invitationIDs);
                string query = "SELECT * FROM Invitations WHERE InvitationID IN " + "(" + combinedStringInvitationIDs + ")";
                SqlCommand command = new SqlCommand(query, sqlConnection);
                SqlDataReader dataReader = command.ExecuteReader();

                List<Invitation> invitations = new List<Invitation>();
                while (dataReader.Read()) {
                    int invitationID = dataReader.GetInt32(0);
                    int eventID = dataReader.GetInt32(1);
                    int teamID = dataReader.GetInt32(2);
                    string senderUsername = dataReader.GetString(3);

                    Invitation curInvite = new Invitation(invitationID, eventID, teamID, senderUsername);
                    invitations.Add(curInvite);
                }

                command.Parameters.Clear(); dataReader.Close();
                sqlConnection.Close(); return invitations;
            }
            catch (Exception exception) { Console.WriteLine("GetEvent: " + exception.Message); throw; }
        }

        public static void RemoveInvitation(int invitationID) {
            try {
                string connectionString = ConnectionUtil.connectionString;
                SqlConnection sqlConnection = new SqlConnection(connectionString);
                sqlConnection.Open();

                string query = "DELETE FROM Invitations WHERE InvitationID = @InvitationID";
                SqlCommand command = new SqlCommand(query, sqlConnection);
                command.Parameters.Add("@InvitatonID", SqlDbType.Int).Value = invitationID;

                SqlDataReader dataReader = command.ExecuteReader();
                command.Parameters.Clear(); dataReader.Close(); sqlConnection.Close();
            }
            catch (Exception exception) { Console.WriteLine("AddInvitation: " + exception.Message); throw; }
        }

        public static void RemoveUserInvitation(string username, int invitationID)
        {
            try {
                string connectionString = ConnectionUtil.connectionString;
                SqlConnection sqlConnection = new SqlConnection(connectionString);
                sqlConnection.Open();

                string query = "DELETE FROM isInvited WHERE Username = @Username AND InvitationID = @InvitationID";
                SqlCommand command = new SqlCommand(query, sqlConnection);
                command.Parameters.Add("@Username", SqlDbType.NVarChar).Value = username;
                command.Parameters.Add("@InvitationID", SqlDbType.Int).Value = invitationID;

                SqlDataReader dataReader = command.ExecuteReader();
                command.Parameters.Clear(); dataReader.Close();

                query = "SELECT Pending FROM Invitations WHERE InvitationID = @InvitationID";
                command = new SqlCommand(query, sqlConnection);
                command.Parameters.Add("@InvitationID", SqlDbType.Int).Value = invitationID;

                dataReader = command.ExecuteReader();
                int pending = dataReader.Read() ? dataReader.GetInt32(0) : 1;
                command.Parameters.Clear(); dataReader.Close();

                query = "UPDATE Invitations SET Pending = @Pending WHERE InvitationID = @InvitationID";
                command = new SqlCommand(query, sqlConnection);
                command.Parameters.Add("@Pending", SqlDbType.Int).Value = pending - 1;
                command.Parameters.Add("@InvitationID", SqlDbType.Int).Value = invitationID;

                dataReader = command.ExecuteReader();
                command.Parameters.Clear(); dataReader.Close(); sqlConnection.Close(); 
                if(pending == 1) { RemoveInvitation(invitationID); }
            }
            catch (Exception exception) { Console.WriteLine("RemoveInvitation: " + exception.Message); throw; }

        }
    }
}
