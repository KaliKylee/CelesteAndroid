// Stub do Steamworks.NET. Só os membros referenciados pelo Celeste 1.4.0.0;
// as assinaturas precisam bater exatamente com as do assembly original.
namespace Steamworks
{
	public struct AppId_t
	{
		public uint m_AppId;

		public AppId_t(uint value)
		{
			m_AppId = value;
		}
	}

	public struct SteamAPICall_t
	{
		public ulong m_SteamAPICall;
	}

	public static class SteamAPI
	{
		public static bool Init() => true;

		public static bool RestartAppIfNecessary(AppId_t unOwnAppID) => false;

		public static void RunCallbacks()
		{
		}
	}

	public static class SteamApps
	{
		public static string GetCurrentGameLanguage() => "english";
	}

	public static class SteamUserStats
	{
		public static bool RequestCurrentStats() => false;

		public static SteamAPICall_t RequestGlobalStats(int nHistoryDays) => default;

		public static bool GetStat(string pchName, out int pData)
		{
			pData = 0;
			return false;
		}

		public static bool GetStat(string pchName, out float pData)
		{
			pData = 0f;
			return false;
		}

		public static bool SetStat(string pchName, int nData) => false;

		public static bool SetStat(string pchName, float fData) => false;

		public static bool GetGlobalStat(string pchStatName, out long pData)
		{
			pData = 0;
			return false;
		}

		public static bool GetGlobalStat(string pchStatName, out double pData)
		{
			pData = 0;
			return false;
		}

		public static bool GetAchievement(string pchName, out bool pbAchieved)
		{
			pbAchieved = false;
			return false;
		}

		public static bool SetAchievement(string pchName) => false;

		public static bool StoreStats() => false;
	}
}
