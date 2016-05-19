using System;
using WoWDaemon.Common;
using WoWDaemon.Common.Attributes;
using WoWDaemon.Login;
using WoWDaemon.Database;
using WoWDaemon.Database.DataTables;
namespace LoginScripts.ChatCommands
{
	/// <summary>
	/// Summary description for Info.
	/// </summary>
	[ChatCmdHandler()]
	public class Info
	{
		[ChatCmdAttribute("info", "No usage.")]
		static bool OnInfo(LoginClient client, string input)
		{
			Chat.System(client, "This server is running WoWDaemon 0.1. Current users: " + LoginServer.CurrentUsers +
				" Top users this uptime: " + LoginServer.TopUsers);
			int accounts = DataServer.Database.SelectAllObjects(typeof(DBAccount)).Length;
			int characters = DataServer.Database.SelectAllObjects(typeof(DBCharacter)).Length;
			Chat.System(client, "Accounts: " + accounts + " Characters: " + characters);
			return true;
		}
	}
}
