using UnityEngine;
using DG.Tweening; // Ease 타입을 위해 추가
using System.Collections.Generic;

// 각 노드의 필드 데이터를 저장하기 위한 부모 클래스 (식별용)
public abstract class BaseNodeFields { }

public class TextNodeFields : BaseNodeFields
{
    public string sceneName;
    public string display;
}

public class BranchNodeFields : BaseNodeFields
{
    public string sceneName;
}

public class WaitNodeFields : BaseNodeFields
{
    public float duration;
}

public class CommentNodeFields : BaseNodeFields
{
    public string comment;
}

public class PortraitEnterNodeFields : BaseNodeFields
{
    public string portraitName;
    public string location;
    public PositionData offset;
    public string face;
    public string animKey;
    public ColorData startTint;
    public ColorData endTint;
    public float duration;
    public Ease ease;
    public bool spotLight;
    public bool skippable;
}

public class PortraitAnimNodeFields : BaseNodeFields
{
    public string portraitName;
    public string location;
    public PositionData offset;
    public string face;
    public string animKey;
    public ColorData endTint;
    public float duration;
    public Ease ease;
    public bool spotLight;
    public bool skippable;
}

public class PortraitExitNodeFields : BaseNodeFields
{
    public string portraitName;
    public string animKey;
    public ColorData endTint;
    public float duration;
    public Ease ease;
    public bool spotLight;
    public bool skippable;
}

public class ImageNodeFields : BaseNodeFields
{
    public string controlType;
    public string display;
    public string imageName;
    public ColorData startTint;
    public ColorData endTint;
    public float duration;
    public Ease ease;
    public bool skippable;
}

public class SoundNodeFields : BaseNodeFields
{
    public string controlType;
    public string display;
    public string soundName;
    public float fadeDuration;
    public float volume;
    public bool isLoop;
    public bool skippable;
}