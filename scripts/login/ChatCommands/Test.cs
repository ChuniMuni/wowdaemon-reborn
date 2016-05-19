using System;
using WoWDaemon.Common;
using WoWDaemon.Common.Attributes;
using WoWDaemon.Login;
namespace LoginScripts.ChatCommands
{
	/// <summary>
	/// Summary description for Test.
	/// </summary>
	[ChatCmdHandler()]
	public class Test
	{
		[ChatCmdAttribute("test", "No usage.")]
		static bool OnTest(LoginClient client, string input)
		{
			Chat.System(client, "Hello again world!");
			return true;
		}
	}
}
