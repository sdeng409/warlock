import lock from './versions-lock.json' with { type: 'json' };

export const VERSION_LOCK = Object.freeze(lock);

export function assertVersionLock(lockCandidate = VERSION_LOCK) {
  const isValid =
    lockCandidate?.engine?.version === '6000.0.68f1' &&
    lockCandidate?.network?.version === '2.0.11 Stable' &&
    lockCandidate?.network?.build === '1743' &&
    lockCandidate?.packages?.['com.unity.inputsystem'] === '1.17.0' &&
    lockCandidate?.packages?.['com.unity.ugui']?.lockType === 'editor-core' &&
    lockCandidate?.packages?.['com.unity.ugui']?.editorVersion === '6000.0.68f1' &&
    lockCandidate?.packages?.uiToolkit?.lockType === 'builtin' &&
    lockCandidate?.packages?.uiToolkit?.unityMajor === 6 &&
    lockCandidate?.engine?.renderPipeline === 'URP';

  return {
    ok: isValid,
    expected: {
      unity: '6000.0.68f1',
      fusion: '2.0.11 Stable',
      fusionBuild: '1743',
      inputSystem: '1.17.0',
      ugui: 'editor-core (Unity 6000.0.68f1)',
      uiToolkit: 'Unity 6 builtin',
      renderPipeline: 'URP',
    },
  };
}
