﻿using DSharpPlus.CommandsNext;
using DSharpPlus.Entities;
using MySql.Data.MySqlClient;
using SuperGamblino.GameObjects;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace SuperGamblino
{
	class Database
	{
		private static string connectionString = "";
		public static void SetConnectionString(string host, int port, string database, string username, string password)
		{
			connectionString = "server=" + host +
							   ";database=" + database +
							   ";port=" + port +
							   ";userid=" + username +
							   ";password=" + password;
		}
		public static MySqlConnection GetConnection()
		{
			return new MySqlConnection(connectionString);
		}

		public static void SetupTables()
		{
			using (MySqlConnection c = GetConnection())
			{
				MySqlCommand createUser = new MySqlCommand(
					"CREATE TABLE IF NOT EXISTS user(" +
					"user_id BIGINT UNSIGNED NOT NULL PRIMARY KEY," +
					"currency INT)",
					c);
				c.Open();
				createUser.ExecuteNonQuery();
			}
		}
	
		public static void SetupProcedures()
		{
			using (MySqlConnection c = GetConnection())
			{
				MySqlCommand createUser = new MySqlCommand(
					"DROP procedure IF EXISTS `get_top_users`; " +
					"CREATE PROCEDURE `get_top_users`() " +
					"BEGIN " +
					"SELECT * " +
					"FROM user " +
					"ORDER BY currency DESC " + 
					"LIMIT 10; " + 
					"END",
					c);
				c.Open();
				createUser.ExecuteNonQuery();
			}
		}

		public static int CommandGetUserCredits(ulong userId)
		{
			using (MySqlConnection c = GetConnection())
			{
				c.Open();
				MySqlCommand selection = new MySqlCommand(@"SELECT currency FROM user WHERE user_id = @user_id", c);
				selection.Parameters.AddWithValue("@user_id", userId);
				selection.Prepare();
				MySqlDataReader results = selection.ExecuteReader();

				if (!results.Read())
				{
					return 0;
				}

				Object currentCredits = results.GetValue(0);
				results.Close();
				return Convert.ToInt32(currentCredits);
			}
		}
		
		public static bool CommandSubsctractCredits(ulong userId, int credits)
		{
				if (CommandGetUserCredits(userId) >= credits)
				{
					CommandGiveCredits(userId, credits * -1);
					return true;
				}
				return false;
		}

		public static async Task<List<User>> CommandGetGlobalTop(CommandContext command)
		{
			List<User> discordUsers = new List<User>();
			//CALL `supergamblino`.`get_top_users`();
			using (MySqlConnection c = GetConnection())
			{
				c.Open();
				MySqlCommand selection = new MySqlCommand(@"CALL `get_top_users`()", c);
				MySqlDataReader results = selection.ExecuteReader();

				

				while (results.Read())
				{
					ulong uid = results.GetUInt64(0);
					int cur = results.GetInt32(1);
					discordUsers.Add(new User { discordUser = await command.Client.GetUserAsync(uid), currency = cur});
				}

				//Object currentCredits = results.GetValue(0);
				results.Close();
			}
			return discordUsers;
		}

		public static int CommandGiveCredits(ulong userId, int credits)
		{
			try
			{
				using (MySqlConnection c = GetConnection())
				{
					MySqlCommand searchCoins = new MySqlCommand(
						@"UPDATE user SET currency = currency + (@credits) WHERE user_id = @userId",
						c);
					c.Open();
					searchCoins.Parameters.AddWithValue("@userId", userId);
					searchCoins.Parameters.AddWithValue("@credits", credits);
					Console.WriteLine(searchCoins.CommandText);
					searchCoins.ExecuteNonQuery();
					return CommandGetUserCredits(userId);
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine(ex.Message);
				return -1;
			}
		}

		public static int CommandSearch(ulong userId)
		{
			Random rnd = new Random();
			int foundMoney = rnd.Next(10, 50);
			try
			{
				using (MySqlConnection c = GetConnection())
				{
					MySqlCommand searchCoins = new MySqlCommand(
						@"INSERT INTO user (user_id, currency) VALUES(@userId, @moneyFound) ON DUPLICATE KEY UPDATE currency = currency + @moneyFound",
						c);
					c.Open();
					searchCoins.Parameters.AddWithValue("@userId", userId);
					searchCoins.Parameters.AddWithValue("@moneyFound", foundMoney);
					Console.WriteLine(searchCoins.CommandText);
					Console.WriteLine(foundMoney);
					searchCoins.ExecuteNonQuery();
					return foundMoney;
				}
			}
			catch(Exception ex)
			{
				Console.WriteLine(ex.Message);
				return -1;
			}

		}

	}
}