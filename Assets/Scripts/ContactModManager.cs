using Unity.Collections;
using UnityEngine;

public class ContactModManager : MonoBehaviour
{
    private enum ContactModType { None, Boost, NoSeparation, Valve };
    private NativeHashMap<int, ContactModType> modifiableColliders;

    void Start()
    {
        modifiableColliders = new NativeHashMap<int, ContactModType>(10, Allocator.Persistent);
        SetupContactModGameObjects(GameObject.FindGameObjectsWithTag("Boost"), ContactModType.Boost);
        SetupContactModGameObjects(GameObject.FindGameObjectsWithTag("NoSeparation"), ContactModType.NoSeparation);
        SetupContactModGameObjects(GameObject.FindGameObjectsWithTag("Valve"), ContactModType.Valve);

        Physics.ContactModifyEvent = ModificationCallback;
    }

    private void SetupContactModGameObjects(GameObject[] gameObjects, ContactModType contactModType)
    {
        foreach (GameObject gameObject in gameObjects)
        {
            Collider collider = gameObject.GetComponent<Collider>();
            if (collider == null)
                continue;
            collider.hasModifiableContacts = true;
            modifiableColliders.Add(collider.GetInstanceID(), contactModType);
        }
    }

    public void ModificationCallback(PhysicsScene scene, NativeArray<ModifiableContactPair> contactPairs)
    {
        foreach (var pair in contactPairs)
        {
            ContactModType colliderModType = ContactModType.None;
            ContactModType otherColliderModType = ContactModType.None;
            modifiableColliders.TryGetValue(pair.colliderInstanceID, out colliderModType);
            modifiableColliders.TryGetValue(pair.otherColliderInstanceID, out otherColliderModType);

            if (colliderModType == ContactModType.Boost || otherColliderModType == ContactModType.Boost)
                ModifyBoostPair(pair);

            if (colliderModType == ContactModType.NoSeparation || otherColliderModType == ContactModType.NoSeparation)
                ModifyNoSeparationPair(pair);

            if (colliderModType == ContactModType.Valve || otherColliderModType == ContactModType.Valve)
                ModifyValvePair(pair);
        }
    }

    private void ModifyBoostPair(ModifiableContactPair pair)
    {
        for (int i = 0; i< pair.contactsCount; i++)
        {
            // set the target velocity to create "boost pad".
            pair.SetTargetVelocity(i, new Vector3(-35, 0, 0));
        }
    }

    private void ModifyNoSeparationPair(ModifiableContactPair pair)
    {
        for (int i = 0; i < pair.contactsCount; i++)
        {
            // ignore contacts that have positive separation to prevent ball from bouncing on collider seams
            if (pair.GetSeparation(i) > 0)
                pair.IgnoreContact(i);
        }
    }

    private void ModifyValvePair(ModifiableContactPair pair)
    {
        for (int i = 0; i < pair.contactsCount; i++)
        {
            // ignore contacts that have right facing normal to create a valve like behaviour
            if (Vector3.Dot(Vector3.right, pair.GetNormal(i)) > 0)
                pair.IgnoreContact(i);
        }
    }
}
