export const SKILL_CATALOG = Object.freeze([
  { id: 'S01', name: 'Arc Push', tag: 'Knockback', castType: 'Direction', cooldownSec: 5, pushForce: 1.0, price: 2 },
  { id: 'S02', name: 'Blast Palm', tag: 'Knockback', castType: 'Target', cooldownSec: 7, pushForce: 1.3, price: 3 },
  { id: 'S03', name: 'Shock Ring', tag: 'Knockback', castType: 'Area', cooldownSec: 10, pushForce: 1.1, price: 4 },
  { id: 'S04', name: 'Overdrive Wave', tag: 'Knockback', castType: 'Direction', cooldownSec: 14, pushForce: 1.6, price: 6 },
  { id: 'S05', name: 'Ember Bolt', tag: 'Damage', castType: 'Direction', cooldownSec: 4, pushForce: 0.2, price: 2 },
  { id: 'S06', name: 'Void Spear', tag: 'Damage', castType: 'Target', cooldownSec: 8, pushForce: 0.1, price: 4 },
  { id: 'S07', name: 'Blink Step', tag: 'Mobility', castType: 'Self', cooldownSec: 10, pushForce: 0.0, price: 3 },
  { id: 'S08', name: 'Dash Burst', tag: 'Mobility', castType: 'Direction', cooldownSec: 12, pushForce: 0.4, price: 4 },
  { id: 'S09', name: 'Slow Field', tag: 'Control', castType: 'Area', cooldownSec: 12, pushForce: 0.0, price: 3 },
  { id: 'S10', name: 'Silence Pulse', tag: 'Control', castType: 'Area', cooldownSec: 15, pushForce: 0.0, price: 5 },
  { id: 'S11', name: 'Guard Shell', tag: 'Defense', castType: 'Self', cooldownSec: 13, pushForce: 0.0, price: 4 },
  { id: 'S12', name: 'Barrier Wall', tag: 'Defense', castType: 'Area', cooldownSec: 16, pushForce: 0.0, price: 5 },
]);

export const SKILL_BY_ID = Object.freeze(Object.fromEntries(SKILL_CATALOG.map((s) => [s.id, s])));
