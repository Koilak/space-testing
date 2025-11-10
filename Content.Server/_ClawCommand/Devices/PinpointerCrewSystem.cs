using Content.Shared._ClawCommand.Devices.Components;
using Content.Shared.Pinpointer;
using Robust.Server.GameObjects;
using Content.Server.Medical.SuitSensors;
using Content.Shared.Medical.SuitSensor;

namespace Content.Server._ClawCommand.Devices.Systems;

public sealed partial class ServerPinpointerCrewSystem : EntitySystem
{
    [Dependency] private readonly UserInterfaceSystem _userInterface = default!;
    [Dependency] private readonly IEntityManager _entityManager = default!;
    [Dependency] private readonly SharedPinpointerSystem _sharedPinpointerSystem = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<PinpointerCrewComponent, BoundUIOpenedEvent>(OpenUIEvent);
        SubscribeLocalEvent<PinpointerComponent, CrewTrackerSelectCrewMessage>(ChangeTarget);

    }
    private float _timePassed = 0;
    public override void Update(float frameTime)
    {
        _timePassed += frameTime;
        if (_timePassed > 3)
        {
            _timePassed = 0;
            var pointers = EntityManager.EntityQueryEnumerator<PinpointerCrewComponent, PinpointerComponent>();
            while (pointers.MoveNext(out var pointerId, out var pinpointerCrewComponent, out var pinpointerComponent))
            {

                if (pinpointerComponent.IsActive == false)
                {
                    continue;
                }

                var sensors = EntityManager.EntityQueryEnumerator<SuitSensorComponent>();
                var found = false;
                while (sensors.MoveNext(out var sensorId, out var sensorComponent))
                {
                    if (sensorComponent.Mode == SuitSensorMode.SensorCords && sensorComponent.User is EntityUid cast3)
                    {
                        if (cast3 == pinpointerComponent.Target)
                        {
                            found = true;
                            break;

                        }
                    }
                }
                if (!found)
                {
                    _sharedPinpointerSystem.SetActive(pointerId, false, pinpointerComponent);
                    _sharedPinpointerSystem.SetTarget(pointerId, null, pinpointerComponent);
                }

            }
        }
    }
    private void ChangeTarget(EntityUid uid, PinpointerComponent pinpointer, CrewTrackerSelectCrewMessage args)
    {
        if (args.ID is not int targetID)
        {
            return;
        }
        var sensors = EntityManager.EntityQueryEnumerator<SuitSensorComponent>();

        while (sensors.MoveNext(out var sensorId, out var sensorComponent))
        {
            if (sensorComponent.Mode == SuitSensorMode.SensorCords && sensorComponent.User is EntityUid cast3)
            {
                _sharedPinpointerSystem.SetTarget(uid, new EntityUid(targetID), pinpointer);
                _sharedPinpointerSystem.SetActive(uid, true, pinpointer);
                /*if (cast3.Id == args.ID)
                {
                    if (!_entityManager.TryGetComponent<MetaDataComponent>(cast3, out var metadataComponent))
                        continue;
                    _sharedPinpointerSystem.SetTarget(uid, new EntityUid(targetID), pinpointer);

                    pinpointer.TargetName = metadataComponent.EntityName;
                }*/
            }
        }
    }

    private void OpenUIEvent(EntityUid uid, PinpointerCrewComponent component, BoundUIOpenedEvent arg)
    {


        if (!_entityManager.TryGetComponent<PinpointerComponent>(uid, out var pinpointerComponent))
            return;

        PinpointedCrew? target = null;
        if (pinpointerComponent.Target is EntityUid cast && pinpointerComponent.TargetName is String cast2)
        {
            target = new PinpointedCrew();
            target.ID = cast.Id;
            target.Name = cast2;
        }
        var sensors = EntityManager.EntityQueryEnumerator<SuitSensorComponent>();

        var crewList = new List<PinpointedCrew>();

        while (sensors.MoveNext(out var sensorId, out var sensorComponent))
        {
            if (sensorComponent.Mode == SuitSensorMode.SensorCords && sensorComponent.User is EntityUid cast3)
            {
                target = new PinpointedCrew();
                target.ID = cast3.Id;

                if (!_entityManager.TryGetComponent<MetaDataComponent>(cast3, out var metadataComponent))
                    continue;
                target.Name = metadataComponent.EntityName;
                crewList.Add(target);
            }
        }

        var newState = new PinpointerCrewBoundUserInterfaceState(target, crewList);

        _userInterface.SetUiState(uid, PinpointerCrewUiKey.Key, newState);


    }

}
