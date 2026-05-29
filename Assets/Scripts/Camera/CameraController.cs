using System;
using UnityEngine;
using Unity.Cinemachine;
using TriInspector;
using System.Collections.Generic;
using UnityEditor.Localization.Plugins.XLIFF.V12;
using System.Linq;
using GameEvents;
using SLTypes;
using UnityConstantsGenerator;
using UnityEditor.Experimental.GraphView;
using UnityServiceLocator;

public class CameraController : MonoBehaviour
{
    public static CameraController Instance { get; private set; }

    [SerializeField, LabelText("Normal Gameplay Camera")] private CinemachineCamera gameplayCam;
    [SerializeField, LabelText("Player Target Group")] private Transform targetGroup;

    [SerializeField, Range(0f, 10000f)] private float raycastRadius = 2f;
    [SerializeField] private Vector3 clearViewOffset = new Vector3(0, 1f, 0);

    private readonly Dictionary<Renderer, Material[]> originalMaterials = new();
    private readonly HashSet<Renderer> currentlyTransparent = new();
    private readonly HashSet<Renderer> hitThisFrame = new();
    [SerializeField] private Material obstructionTransparentMaterial;

    [SerializeField] private List<LayerId> layersToExclude;
    private List<int> _layersToExclude = new();

    private IPlayer player;

    private void Awake()
    {
        // Singleton-Pattern
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        foreach(LayerId layer in layersToExclude)
        {
            _layersToExclude.Add((int)layer);
        }
    }

    private void Start()
    {
        ServiceLocator.Global.Get(out player);
    }

    private void OnDrawGizmosSelected()
    {
        if (targetGroup == null)
            return;

        CinemachineTargetGroup group = targetGroup.GetComponent<CinemachineTargetGroup>();

        if (group == null || group.Targets.Count < 1 || group.Targets[0].Object == null)
            return;

        // player position also:    player.Position
        Vector3 playerPos = group.Targets[0].Object.position + clearViewOffset;

        Vector3 origin = transform.position;
        Vector3 direction = playerPos - origin;
        float distance = direction.magnitude - 0.2f;

        if (distance <= 0f)
            return;

        direction.Normalize();

        Vector3 end = origin + direction * distance;

        Gizmos.color = Color.yellow;

        // Start and end spheres of the SphereCast
        Gizmos.DrawWireSphere(origin, raycastRadius);
        Gizmos.DrawWireSphere(end, raycastRadius);

        // Center line
        Gizmos.DrawLine(origin, end);

        // Approximate capsule sides
        Vector3 right = Vector3.Cross(direction, Vector3.up).normalized;

        if (right == Vector3.zero)
            right = Vector3.Cross(direction, Vector3.forward).normalized;

        Vector3 up = Vector3.Cross(right, direction).normalized;

        Gizmos.DrawLine(origin + right * raycastRadius, end + right * raycastRadius);
        Gizmos.DrawLine(origin - right * raycastRadius, end - right * raycastRadius);
        Gizmos.DrawLine(origin + up * raycastRadius, end + up * raycastRadius);
        Gizmos.DrawLine(origin - up * raycastRadius, end - up * raycastRadius);
    }

    private void Update()
    {
        ClearViewBlockingObjects();
    }

    private void ClearViewBlockingObjects()
    {
        List<CinemachineTargetGroup.Target> targets = targetGroup.GetComponent<CinemachineTargetGroup>().Targets;
        if(targets.Count < 1)
            return;

        // player position also:    player.Position
        Vector3 playerPos = targets[0].Object.position + clearViewOffset;  // get position (with offset for body/head)

        Vector3 direction = playerPos - transform.position;
        float distance = Vector3.Distance(playerPos, transform.position);
        direction.Normalize();

        // Raycast from Camera to Player with length = distance - buffer
        RaycastHit[] hits = Physics.SphereCastAll(
            transform.position,
            raycastRadius,
            direction,
            distance - 0.2f
        );

        hitThisFrame.Clear();

        // deactivate Renderers of intercepting objects
        for(int i = 0; i < hits.Length; i++)
        {
            GameObject obj = hits[i].collider.gameObject;

            if (_layersToExclude.Contains(obj.layer))
                continue;

            if (player.CurrentGrounds.Contains(obj))
                continue;

            Renderer[] renderers = obj.GetComponentsInChildren<Renderer>();

            foreach (Renderer renderer in renderers)
            {
                MakeTransparent(renderer);
                hitThisFrame.Add(renderer);
            }
        }
        
        RestoreNoLongerBlockingRenderers();
    }

    private void MakeTransparent(Renderer renderer)
    {
        if (renderer == null)
            return;

        if (!originalMaterials.ContainsKey(renderer))
        {
            originalMaterials[renderer] = renderer.sharedMaterials;
        }

        Material[] replacementMaterials = new Material[renderer.sharedMaterials.Length];

        for (int i = 0; i < replacementMaterials.Length; i++)
        {
            replacementMaterials[i] = obstructionTransparentMaterial;  // set to transparent material
        }

        renderer.sharedMaterials = replacementMaterials;
        currentlyTransparent.Add(renderer);
    }

    private void RestoreNoLongerBlockingRenderers()
    {
        List<Renderer> toRestore = new();

        // collect Render-Materials that need to be restored
        foreach (Renderer renderer in currentlyTransparent)
        {
            if (!hitThisFrame.Contains(renderer))
            {
                toRestore.Add(renderer);
            }
        }

        // restore Render-Materials that are not intercepting anymore
        foreach (Renderer renderer in toRestore)
        {
            if (renderer != null && originalMaterials.TryGetValue(renderer, out Material[] materials))
            {
                renderer.sharedMaterials = materials;
            }

            originalMaterials.Remove(renderer);
            currentlyTransparent.Remove(renderer);
        }
    }
    

    /*public void SwitchToGameplay()
    {
        gameplayCam.Priority = 20;
    }

    public void SwitchToOnShoulder()
    {
        gameplayCam.Priority = 10;
    }

    public void SwitchToHighlight()
    {
        gameplayCam.Priority = 10;
    }*/
}

