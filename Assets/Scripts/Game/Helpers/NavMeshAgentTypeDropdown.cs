using TriInspector;
using UnityEngine.AI;

public static class NavMeshAgentTypeDropdown
{
    public static TriDropdownList<int> GetAgentTypes()
    {
        var dropdown = new TriDropdownList<int>();

        int count = NavMesh.GetSettingsCount();

        for (int i = 0; i < count; i++)
        {
            NavMeshBuildSettings settings = NavMesh.GetSettingsByIndex(i);
            int id = settings.agentTypeID;

            string name = NavMesh.GetSettingsNameFromID(id);

            if (string.IsNullOrEmpty(name))
                name = $"Agent Type {id}";

            dropdown.Add(name, id);
        }

        return dropdown;
    }
}