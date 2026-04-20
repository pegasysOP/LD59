using UnityEngine;

/// <summary>
/// Audio configuration for the Monster Minigame. Covers:
/// <list type="bullet">
///   <item><description><b>Appearance stinger</b> — 2D one-shot fired the moment the creature reveals
///   itself (minigame starts). Sits on top of whatever music is playing.</description></item>
///   <item><description><b>Monster syllables</b> — pool of clicks/chatter played for each beat of the
///   alien-demonstrated pattern. Played attached to the 3D monster so they track its position and
///   facing.</description></item>
///   <item><description><b>Monster idle vocals</b> — background creature vocalisations (grumbles,
///   chatter, etc.) fired on a random interval while the monster is NOT speaking its syllable
///   pattern, so it feels alive without stepping on the pattern the player has to repeat. Future
///   mood-specific pools (happy / unhappy) are expected to live alongside this generic one.</description></item>
///   <item><description><b>Player snap</b> — 2D finger-snap/click played on every player input
///   (hit or stray miss) to give tactile feedback without pretending to come from a point in
///   space.</description></item>
/// </list>
/// The stinger is a single <see cref="AudioClipVolume"/> because it's a signature moment; the
/// syllable / idle-vocals / snap entries are <see cref="SfxBank"/>s so they can be randomised out
/// of a pool and gain pitch/volume jitter for organic variety.
/// </summary>
[CreateAssetMenu(menuName = "Audio/Monster Minigame Sounds", fileName = "MonsterMinigameSounds")]
public class MonsterMinigameSounds : ScriptableObject
{
    [Header("Appearance Stinger")]
    [Tooltip("2D one-shot fired once when the Monster Minigame begins and the creature first appears. " +
             "Designed to sit on top of the music bed as a reveal cue — not spatialised on purpose.")]
    public AudioClipVolume monsterAppearStinger;

    [Header("Appearance Roar")]
    [Tooltip("Pool of diegetic roar clips played at the 3D monster's position the moment it appears, " +
             "layered on top of the 2D stinger. The stinger sells the cinematic punch; the roar sells " +
             "'the creature is right there' and reads in the correct direction/distance from the player.")]
    public SfxBank monsterAppearRoar = new SfxBank { pitchMin = 0.95f, pitchMax = 1.05f };

    [Tooltip("Seconds the Monster Minigame holds before beginning the first 'Listen...' round after the " +
             "session starts. Lets the appearance stinger + roar breathe and the player register the " +
             "creature before the call-and-response gameplay kicks in.")]
    [Min(0f)] public float entranceSettleDelay = 4f;

    [Header("Monster Approach Hop")]
    [Tooltip("Pool of hop/stomp/lunge clips played at the 3D monster each time it closes the distance " +
             "on a failed round. Attached to the monster transform so the hop tracks its motion for " +
             "the clip's lifetime. Fires once per fail-step (not on the final attack lunge — that's " +
             "handled by the attack animation / game-over cue).")]
    public SfxBank monsterHop = new SfxBank { pitchMin = 0.95f, pitchMax = 1.05f };

    [Header("Monster Happy Reaction (correct response)")]
    [Tooltip("Pool of 'pleased/approving' creature vocalisations fired after the player nails the " +
             "response, while the monster is still on-screen. Attached to the 3D monster so it reads " +
             "as the creature reacting to the player before it departs.")]
    public SfxBank monsterHappyReaction = new SfxBank { pitchMin = 0.97f, pitchMax = 1.03f };
    [Tooltip("Seconds the game holds on the happy reaction after playing it, before the vanish cue " +
             "fires and the monster is dismissed. Give the reaction room to breathe so the success " +
             "beat lands emotionally.")]
    [Min(0f)] public float happyReactionHoldDuration = 1.5f;

    [Header("Monster Vanish (exit)")]
    [Tooltip("Pool of 'poof/depart/teleport-away' cues fired at the monster's current world position " +
             "the moment it disappears at the end of a winning session. Uses a baked world position " +
             "(not attached) so the clip keeps ringing out after the monster GameObject is " +
             "deactivated by the panel hide.")]
    public SfxBank monsterVanish = new SfxBank { pitchMin = 0.97f, pitchMax = 1.03f };

    [Header("Monster Unhappy Reaction (wrong response)")]
    [Tooltip("Pool of 'displeased/frustrated' creature vocalisations fired after the player botches a " +
             "response (non-lethal fail) and the monster has already taken its step closer. Attached to " +
             "the 3D monster. Plays BEFORE the next round begins so the player can feel the monster's " +
             "disapproval before being asked to try again.")]
    public SfxBank monsterUnhappyReaction = new SfxBank { pitchMin = 0.95f, pitchMax = 1.05f };
    [Tooltip("Seconds the game holds on the unhappy reaction after playing it, before the next round's " +
             "'Translating...' phase begins. Stacks on top of the existing post-response delay so you " +
             "can tune the reaction window independently of the round-restart pacing.")]
    [Min(0f)] public float unhappyReactionHoldDuration = 1.0f;

    [Header("Monster Syllables (alien pattern beats)")]
    [Tooltip("Pool of monster syllable one-shots played for each beat of the alien-demonstrated pattern. " +
             "Plays attached to the 3D monster so syllables come from the creature as it moves.")]
    public SfxBank monsterSyllables = new SfxBank { pitchMin = 0.9f, pitchMax = 1.1f };
    [Tooltip("How many syllable one-shots to stack per pattern beat. Each stacked instance re-rolls the " +
             "bank independently (random clip + random pitch), so raising this thickens the voice and " +
             "makes it louder/chorused without clipping any single AudioSource. 1 = vanilla single-shot, " +
             "2-3 = layered creature vocalisation, 6-8 = chorused roar that punches through the mix.")]
    [Range(1, 8)] public int syllableInstancesPerTrigger = 2;

    [Header("Monster Idle Vocals (ambient between lines)")]
    [Tooltip("Pool of background creature vocalisations (generic idle grumbles/chatter) fired at random " +
             "intervals whenever the monster is NOT currently speaking its syllable pattern. Attached " +
             "to the 3D monster so the ambience reads as the creature vocalising, not generic room tone. " +
             "Mood-specific vocals (happy / unhappy reactions) will plug in as sibling pools later.")]
    public SfxBank monsterIdleVocals = new SfxBank { pitchMin = 0.95f, pitchMax = 1.05f };
    [Tooltip("Minimum seconds between idle vocals while the monster is not speaking.")]
    [Min(0f)] public float idleVocalsMinInterval = 2.5f;
    [Tooltip("Maximum seconds between idle vocals while the monster is not speaking.")]
    [Min(0f)] public float idleVocalsMaxInterval = 6f;
    [Tooltip("Initial delay before the FIRST idle vocal can fire once the minigame begins. Prevents the " +
             "idle pool from overlapping the appearance stinger on session start.")]
    [Min(0f)] public float idleVocalsInitialDelay = 1.5f;

    [Header("Monster Death (final fail — monster kills player)")]
    [Tooltip("2D one-shot stinger fired the moment the monster lands its killing blow on the final fail. " +
             "Mirrors the appearance stinger: non-spatial, sits on top of everything as a signature beat.")]
    public AudioClipVolume monsterDeathStinger;

    [Tooltip("Seconds the BGM (MusicManager) takes to fade to silence once the death sequence starts. " +
             "Music is suspended for the rest of the game-over beat — subsequent calls to " +
             "PlayGameMusic are ignored until the scene reloads / session restarts.")]
    [Min(0f)] public float deathMusicFadeOutDuration = 1.5f;

    [Tooltip("Array of one-shots triggered simultaneously at the moment of death. Each clip's per-entry " +
             "Delay (positive = wait before play, negative = start partway in) is what staggers the " +
             "sequence — the manager fires the whole array in one call and lets the delays do the timing. " +
             "Played 2D (non-spatialised) so the death moment reads as a cinematic beat regardless of " +
             "where the monster ends up relative to the camera.")]
    public AudioClipVolume[] monsterDeathSounds;

    [Header("Player Snap / Click")]
    [Tooltip("Pool of player finger-snap/click one-shots fired on every player input (hit OR stray miss). " +
             "Played 2D — this is a non-diegetic 'you clicked' feedback, not a sound emitting from the world.")]
    public SfxBank playerSnap = new SfxBank { pitchMin = 0.95f, pitchMax = 1.05f };
    [Tooltip("How many snap one-shots to stack per player input. Each stacked instance re-rolls the bank " +
             "independently (random clip + random pitch), thickening the snap and pushing it above the mix " +
             "ceiling without clipping any single AudioSource. 1 = vanilla single-shot, 2-3 = punchier feel, " +
             "6-8 = really aggressive stack for when the snap needs to cut through.")]
    [Range(1, 8)] public int snapInstancesPerTrigger = 1;

    /// <summary>
    /// Fires the 2D appearance stinger through the shared SFX pipe. Safe to call even if
    /// the clip is unassigned (no-op).
    /// </summary>
    public void PlayMonsterAppearStinger()
    {
        PlayStinger2D(monsterAppearStinger);
    }

    /// <summary>
    /// Fires the 2D death stinger through the shared SFX pipe on the final fail. Safe to
    /// call even if the clip is unassigned (no-op).
    /// </summary>
    public void PlayMonsterDeathStinger()
    {
        PlayStinger2D(monsterDeathStinger);
    }

    /// <summary>
    /// Triggers every entry in <see cref="monsterDeathSounds"/> as a 2D one-shot in a single call.
    /// Per-entry <see cref="AudioClipVolume.Delay"/> values stagger the sequence — positive delays
    /// wait before playing; negative delays start partway into the clip. Safe to call with an
    /// empty / null array (no-op).
    /// </summary>
    public void PlayMonsterDeathSounds()
    {
        if (monsterDeathSounds == null || AudioManager.Instance == null)
            return;

        for (int i = 0; i < monsterDeathSounds.Length; i++)
            PlayStinger2D(monsterDeathSounds[i]);
    }

    // Shared 2D one-shot path used by every signature stinger / cue on this ScriptableObject:
    // treats the authored volume as perceived loudness (matches SfxBank) and routes through the
    // ISOLATED-source pipeline so no concurrent pitch-jittered bank can bleed pitch onto the
    // still-ringing stinger by mutating the shared AudioSource's pitch mid-playback.
    private static void PlayStinger2D(AudioClipVolume entry)
    {
        if (entry == null || entry.Clip == null || AudioManager.Instance == null)
            return;

        float linear = AudioVolume.ToLinear(entry.Volume);
        AudioClipVolume shaped = new AudioClipVolume(entry.Clip, linear, entry.Delay);
        AudioManager.Instance.PlaySfxIsolated2D(shaped);
    }
}
