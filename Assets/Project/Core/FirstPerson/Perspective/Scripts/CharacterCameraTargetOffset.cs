﻿using MoreMountains.Tools;
using MoreMountains.TopDownEngine;
using UnityEngine;

public class CharacterCameraTargetOffset : MonoBehaviour, MMEventListener<PerspectiveChangeEvent>
{
    public Vector3 Value = new Vector3(0,2.5f,0);
    private MoreMountains.TopDownEngine.Character _character;
    private void Awake()
    {
        _character = GetComponent<MoreMountains.TopDownEngine.Character>();
        this.MMEventStartListening();
    }
    
    private void OnDestroy()
    {
        this.MMEventStopListening();
    }

    private void Start()
    {
        _character.CameraTarget.transform.localPosition = Value;
    }

    public void OnMMEvent(PerspectiveChangeEvent perspectiveChangeEvent)
    {
        _character.CameraTarget.transform.localPosition = _character.CameraTarget.transform.localPosition.MMSetX(0).MMSetZ(0);
    }
}
