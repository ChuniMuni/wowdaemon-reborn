using System;
using System.Reflection;
using System.Collections;
using WoWDaemon.Common;

namespace WoWDaemon.Login
{
	public interface ILoginServerPacketHandler
	{
		void HandlePacket(WorldConnection connection, WORLDMSG msgID, BinReader data);
	}

	public interface ILoginClientPacketHandler
	{
		bool HandlePacket(LoginClient client, CMSG msgID, BinReader data);
	}

	public delegate void LoginServerPacketDelegate(WorldConnection connection, WORLDMSG msgID, BinReader data);
	public delegate bool LoginClientPacketDelegate(LoginClient client, CMSG msgID, BinReader data);

	/// <summary>
	/// All classes that wants to be automaticly registered needs to have this
	/// attribute, whether or not it's ILoginXXXXPacketHandler or LoginXXXXPacketDelegate
	/// </summary>
	[AttributeUsage(AttributeTargets.Class, AllowMultiple=true)]
	public class LoginPacketHandler : Attribute
	{
		bool m_useDelegates;
		int  m_msgID;
		bool m_clientMessage;
		public LoginPacketHandler(WORLDMSG msgID)
		{
			m_useDelegates = false;
			m_msgID = (int)msgID;
			m_clientMessage = false;
		}
		public LoginPacketHandler(CMSG msgID)
		{
			m_useDelegates = false;
			m_msgID = (int)msgID;
			m_clientMessage = true;
		}

		public LoginPacketHandler()
		{
			m_useDelegates = true;
		}

		public int MsgID
		{
			get { return m_msgID;}
		}

		public bool UseDelegates
		{
			get { return m_useDelegates;}
		}

		public bool ClientMessage
		{
			get { return m_clientMessage;}
		}
	}

	/// <summary>
	/// Make sure the method has the correct corresponding arguments
	/// </summary>
	[AttributeUsage(AttributeTargets.Method, AllowMultiple=true)]
	public class LoginPacketDelegate : Attribute
	{
		int  m_msgID;
		bool m_clientMessage;
		public LoginPacketDelegate(WORLDMSG msgID)
		{
			m_msgID = (int)msgID;
			m_clientMessage = false;
		}
		public LoginPacketDelegate(CMSG msgID)
		{
			m_msgID = (int)msgID;
			m_clientMessage = true;
		}

		public int MsgID
		{
			get { return m_msgID;}
		}

		public bool ClientMessage
		{
			get { return m_clientMessage;}
		}
	}

	/// <summary>
	/// Summary description for LoginPacketManager.
	/// </summary>
	public class LoginPacketManager
	{
		private LoginPacketManager()
		{

		}

		public static void SearchAssemblyForHandlers(Assembly assembly)
		{
			foreach(Type type in assembly.GetTypes())
			{
				if(type.IsClass == false)
					continue;
				LoginPacketHandler[] attribs = (LoginPacketHandler[])type.GetCustomAttributes(typeof(LoginPacketHandler), true);
				foreach(LoginPacketHandler attrib in attribs)
				{
					if(attrib.UseDelegates)
						SearchForDelegates(type);
					else
					{
						if(attrib.ClientMessage)
						{
							if(type.GetInterface(typeof(ILoginClientPacketHandler).ToString()) != null)
							{
								object obj = GetHandlerObject(type);
								RegisterPacketHandler((CMSG)attrib.MsgID, (ILoginClientPacketHandler)obj);
							}
						}
						else
						{
							if(type.GetInterface(typeof(ILoginServerPacketHandler).ToString()) != null)
							{
								object obj = GetHandlerObject(type);
								RegisterPacketHandler((WORLDMSG)attrib.MsgID, (ILoginServerPacketHandler)obj);
							}
						}

					}
				}
			}
		}
		
		static Hashtable handlerObjects = new Hashtable();
		static object GetHandlerObject(Type type)
		{
			if(handlerObjects.Contains(type))
				return handlerObjects[type];
			object obj = Activator.CreateInstance(type);
			handlerObjects[type] = obj;
			return obj;
		}

		static void SearchForDelegates(Type type)
		{			
			MethodInfo[] methods = type.GetMethods(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static);
			foreach(MethodInfo method in methods)
			{
				LoginPacketDelegate[] attribs = (LoginPacketDelegate[])method.GetCustomAttributes(typeof(LoginPacketDelegate), true);
				if(attribs.Length == 0)
					continue;
				if(method.IsStatic)
				{
					foreach(LoginPacketDelegate attrib in attribs)
					{
						if(attrib.ClientMessage)
						{
							LoginClientPacketDelegate wcpd = (LoginClientPacketDelegate)Delegate.CreateDelegate(typeof(LoginClientPacketDelegate), method);
							RegisterPacketHandler((CMSG)attrib.MsgID, wcpd);
						}
						else
						{
							LoginServerPacketDelegate wspd = (LoginServerPacketDelegate)Delegate.CreateDelegate(typeof(LoginServerPacketDelegate), method);
							RegisterPacketHandler((WORLDMSG)attrib.MsgID, wspd);
						}
					}
				}
				else
				{
					object obj = GetHandlerObject(type);
					foreach(LoginPacketDelegate attrib in attribs)
					{
						if(attrib.ClientMessage)
						{
							LoginClientPacketDelegate wcpd = (LoginClientPacketDelegate)Delegate.CreateDelegate(typeof(LoginClientPacketDelegate), obj, method.Name);
							RegisterPacketHandler((CMSG)attrib.MsgID, wcpd);
						}
						else
						{
							LoginServerPacketDelegate wspd = (LoginServerPacketDelegate)Delegate.CreateDelegate(typeof(LoginServerPacketDelegate), obj, method.Name);
							RegisterPacketHandler((WORLDMSG)attrib.MsgID, wspd);
						}
					}
				}
			}
		}

		static Hashtable loginServerHandlers = new Hashtable();
		static Hashtable loginClientHandlers = new Hashtable();
		static Hashtable loginServerDelegates = new Hashtable();
		static Hashtable loginClientDelegates = new Hashtable();
		public static void RegisterPacketHandler(WORLDMSG msgID, ILoginServerPacketHandler handler)
		{
			if(loginServerHandlers.Contains(msgID))
				throw new Exception("There's already a loginserver packet handler for " + msgID);
			loginServerHandlers[msgID] = handler;
		}

		public static void RegisterPacketHandler(WORLDMSG msgID, LoginServerPacketDelegate wspd)
		{
			if(loginServerDelegates[(int)msgID] != null)
			{
				LoginServerPacketDelegate dele = (LoginServerPacketDelegate)loginServerDelegates[(int)msgID];
				loginServerDelegates[(int)msgID] = dele + wspd;
			}
			else
				loginServerDelegates[(int)msgID] = wspd;
		}

		public static void RegisterPacketHandler(CMSG msgID, ILoginClientPacketHandler handler)
		{
			if(loginClientHandlers.Contains(msgID))
				throw new Exception("There's already a loginclient packet handler for " + msgID);
			loginClientHandlers[msgID] = handler;
		}

		public static void RegisterPacketHandler(CMSG msgID, LoginClientPacketDelegate lcpd)
		{
			if(loginClientDelegates[(int)msgID] != null)
			{
				LoginClientPacketDelegate dele = (LoginClientPacketDelegate)loginClientDelegates[(int)msgID];
				loginClientDelegates[(int)msgID] = dele + lcpd;
			}
			else
				loginClientDelegates[(int)msgID] = lcpd;
		}

		public static void UnregisterPacketHandler(WORLDMSG msgID)
		{
			loginServerHandlers.Remove(msgID);
		}

		public static void UnregisterPacketHandler(WORLDMSG msgID, LoginServerPacketDelegate wspd)
		{
			LoginServerPacketDelegate dele = (LoginServerPacketDelegate)loginServerDelegates[(int)msgID];
			loginServerDelegates[(int)msgID] = dele - wspd;
		}

		public static void UnregisterPacketHandler(CMSG msgID)
		{
			loginClientHandlers.Remove(msgID);
		}

		public static void UnregisterPacketHandler(CMSG msgID, LoginClientPacketDelegate wcpd)
		{
			LoginClientPacketDelegate dele = (LoginClientPacketDelegate)loginClientDelegates[(int)msgID];
			loginClientDelegates[(int)msgID] = dele - wcpd;
		}


		public static void HandlePacket(WorldConnection connection, WORLDMSG msgID, BinReader data)
		{
			ILoginServerPacketHandler handler = (ILoginServerPacketHandler)loginServerHandlers[msgID];
			if(handler != null)
				handler.HandlePacket(connection, msgID, data);
			LoginServerPacketDelegate wspd = (LoginServerPacketDelegate)loginServerDelegates[(int)msgID];
			if(wspd != null)
				wspd(connection, msgID, data);
		}

		/// <summary>
		/// Returns false if the packet should be sent to the loginserver
		/// </summary>
		/// <param name="client"></param>
		/// <param name="msgID"></param>
		/// <param name="data"></param>
		/// <returns></returns>
		public static bool HandlePacket(LoginClient client, CMSG msgID, BinReader data)
		{
			if(msgID >= CMSG.MAX)
				return true;

			ILoginClientPacketHandler handler = (ILoginClientPacketHandler)loginClientHandlers[msgID];
			bool wasHandled = false;
			if(handler != null)
				wasHandled = handler.HandlePacket(client, msgID, data);
			LoginClientPacketDelegate wcpd = (LoginClientPacketDelegate)loginClientDelegates[(int)msgID];
			if(wcpd != null)
			{
				foreach(LoginClientPacketDelegate d in wcpd.GetInvocationList())
				{
					if(d(client, msgID, data))
						wasHandled = true;
				}
			}
			return wasHandled;
		}
	}
}