using System.Collections.Generic;
using SLTypes;
using UnityEngine;

public static class ExplosionQuery
{
    public struct Lifeform
    {
        public ILife life;
        public IHuman human;
        public float distance;

        public Lifeform(ILife life, IHuman human, float distance)
        {
            this.life = life;
            this.human = human;
            this.distance = distance;
        }
    }
    
    public static List<Lifeform> GetLifeformsInRadius(
        Vector3 center,
        float radius,
        LayerMask layerMask,
        QueryTriggerInteraction triggerInteraction = QueryTriggerInteraction.Collide)
    {
        Collider[] hits = Physics.OverlapSphere(
            center,
            radius,
            layerMask,
            triggerInteraction);

        var lifes = new List<Lifeform>();
        var seen = new HashSet<MonoBehaviour>();

        foreach (Collider hit in hits)
        {
            // Collider may be on a child object, so search upward.
            MonoBehaviour[] behaviours = hit.GetComponentsInParent<MonoBehaviour>();

            foreach (MonoBehaviour behaviour in behaviours)
            {
                if (behaviour is ILife life && seen.Add(behaviour))
                {
                    float distance = Vector3.Distance(life.Position, center);
                    
                    IHuman human = behaviour as IHuman;  // is null when only ILife or other classes are inherited
                    lifes.Add(new Lifeform(life, human, distance));
                }
            }
        }

        return lifes;
    }
    
    public static List<Lifeform> GetHumansInRadius(
        Vector3 center,
        float radius,
        LayerMask layerMask,
        QueryTriggerInteraction triggerInteraction = QueryTriggerInteraction.Collide)
    {
        Collider[] hits = Physics.OverlapSphere(
            center,
            radius,
            layerMask,
            triggerInteraction);

        var humans = new List<Lifeform>();
        var seen = new HashSet<MonoBehaviour>();

        foreach (Collider hit in hits)
        {
            // Collider may be on a child object, so search upward.
            MonoBehaviour[] behaviours = hit.GetComponentsInParent<MonoBehaviour>();

            foreach (MonoBehaviour behaviour in behaviours)
            {
                if (behaviour is IHuman human && seen.Add(behaviour))
                {
                    float distance = Vector3.Distance(human.Position, center);
                    humans.Add(new Lifeform(human, human, distance));
                }
            }
        }

        return humans;
    }
}