using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

// There's probably a better way of doing this: https://gamedev.stackexchange.com/questions/110958/what-is-the-proper-way-to-handle-data-between-scenes
// See Inversion of Control / Dependency Injection frameworks
// HOWEVER -- maybe that's overkill for this project
//C: Stolen from slider, modified as needed 

public class SaveSystem 
{
    // returns the current SaveProfile being used
    public static SaveProfile Current {
        get {
            if (current == null && TileSelector.Instance != null) // TileSelector.current != null meas we are in play
            {
                Debug.LogWarning("[File IO] Save System is not using a profile! Creating a default profile for now...");
                current = new SaveProfile("Marmalade");
                currentIndex = -1;
            }
            return current;
        }
        private set {
            current = value;
        }
    }
    private static SaveProfile current;
    private static int currentIndex = -1; // if -1, then it's a temporary profile

    public static int maxSaves = 50;
    private static SaveProfile[] saveProfiles = new SaveProfile[maxSaves];

    public SaveSystem()
    {
        for(int i = 0; i < maxSaves; i++)
            SetProfile(i, GetSerializableSaveProfile(i)?.ToSaveProfile());
    }

    public static SaveProfile GetProfile(int index)
    {
        if (index == -1)
            return null;
        return saveProfiles[index];
    }

    public static SaveProfile[] GetProfiles()
    {
        return saveProfiles;
    }

    public static int GetRecentlyPlayedIndex()
    {
        int ret = -1;
        System.DateTime mostRecent = System.DateTime.MinValue;
        for (int i = 0; i < maxSaves; i++)
        {
            if (saveProfiles[i] != null && saveProfiles[i].GetLastSaved() > mostRecent)
            {
                ret = i;
                mostRecent = saveProfiles[i].GetLastSaved();
            }
        }
        return ret;
    }

    public static bool IsCurrentProfileNull()
    {
        return current == null;
    }

    public static void SetProfile(int index, SaveProfile profile)
    {
        saveProfiles[index] = profile;
    }

    public static void SetCurrentProfile(int index)
    {
        currentIndex = index;
        Current = GetProfile(index);
    }

    public static int CreateNewProfile(string name)
    {
        int index = -1;
        for (int i = 0; i < maxSaves; i++)
        {
            if (saveProfiles[i] == null)
            {
                index = i;
                break;
            }
        }
        if(index == -1)
            Debug.Log("Reached Maximum Save Profiles");
        
        SaveProfile newprofile = new SaveProfile(name);
        saveProfiles[index] = newprofile;
        SetCurrentProfile(index);
        SaveGame("Made new profile");

        Debug.Log($"Created profile {name} at index {index}");
        return index;
    }

    public static void SetMostRecentProfile()
    {
        SetCurrentProfile(GetRecentlyPlayedIndex());
    }

    /// <summary>
    /// Saves the game to the current loaded profile index. If the profile index is -1, then no data will be saved.
    /// </summary>
    public static void SaveGame(string reason="")
    {
        if (currentIndex == -1)
            return;
        
        if (reason != "") Debug.Log($"[Saves] Saving game: {reason}");

        current.SetLastSaved(System.DateTime.Now);

        SerializableSaveProfile profile = SerializableSaveProfile.FromSaveProfile(current);

        SaveToFile(profile, currentIndex);
    }

    private static void SaveToFile(SerializableSaveProfile profile, int index)
    {
        Debug.Log($"[File IO] Saving data to file {index}.");

        BinaryFormatter formatter = new BinaryFormatter();
        string path = GetFilePath(index);
        FileStream stream = new FileStream(path, FileMode.Create);

        formatter.Serialize(stream, profile);

        // in case we need json somewhere in the future? idk
        bool doJson = false;
        if (doJson)
        {
            string json = JsonUtility.ToJson(profile);
            Debug.Log(json);

            StreamWriter sr = new StreamWriter(stream);
            sr.WriteLine(json);
            sr.Close();
        }

        stream.Close();
    }

    public static void LoadSaveProfile(int index)
    {
        
        SerializableSaveProfile ssp = null;

        ssp = GetSerializableSaveProfile(index);

        SaveProfile profile;
        if (ssp == null)
        {
            Debug.LogError($"Creating a new temporary save profile -- this shouldn't happen! \n Index:{index}");
            profile = new SaveProfile("Marmalade");
        }
        else
        {
            profile = ssp.ToSaveProfile();
        }

        current = profile;
        currentIndex = index;
    }

    public static SerializableSaveProfile GetSerializableSaveProfile(int index)
    {
        return LoadFromFile(index);
    }

    private static SerializableSaveProfile LoadFromFile(int index)
    {
//        Debug.Log($"[File IO] Loading data from file {index}.");

        string path = GetFilePath(index);
        if (File.Exists(path))
        {
            BinaryFormatter formatter = new BinaryFormatter();
            FileStream stream = new FileStream(path, FileMode.Open);

            SerializableSaveProfile profile = formatter.Deserialize(stream) as SerializableSaveProfile;
            stream.Close();

            return profile;
        }
        else
        {
//            Debug.LogWarning($"[File IO] Save file not found at {path}"); C: Silence debug
            return null;
        }
    }

    public static void DeleteSaveProfile(int index)
    {
        Debug.Log($"[File IO] Deleting Save profile #{index}!");

        saveProfiles[index] = null;

        string path = GetFilePath(index);
        if (File.Exists(path))
        {
            File.Delete(path);
        }
    }

    public static string GetFilePath(int index)
    {
        return Application.persistentDataPath + string.Format("/barkane{0}.marm", index);
    }
}
