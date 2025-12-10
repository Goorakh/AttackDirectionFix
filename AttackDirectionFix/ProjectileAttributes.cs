using HG;
using RoR2;
using RoR2.Projectile;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace AttackDirectionFix
{
    public static class ProjectileAttributes
    {
        static HashSet<GameObject> _collectingProjectilePrefabsBlacklist = [];
        static HashSet<string> _collectingProjectileNamesBlacklist = [];

        static int[] _projectileBlacklist = [];

        static bool[] _isStationaryLookup = [];

        [SystemInitializer(typeof(ProjectileCatalog))]
        static void Init()
        {
            if (ProjectileCatalog.projectilePrefabCount > 0)
            {
                _isStationaryLookup = new bool[ProjectileCatalog.projectilePrefabCount];

                for (int i = 0; i < ProjectileCatalog.projectilePrefabCount; i++)
                {
                    GameObject projectilePrefab = ProjectileCatalog.GetProjectilePrefab(i);
                    if (!projectilePrefab)
                        continue;

                    bool isMoving = (projectilePrefab.TryGetComponent(out ProjectileSimple simple) && (simple.desiredForwardSpeed > 0f || simple.oscillate)) ||
                                    (projectilePrefab.TryGetComponent(out BoomerangProjectile boomerang) && boomerang.travelSpeed > 0f) ||
                                    (projectilePrefab.TryGetComponent(out CleaverProjectile cleaver) && cleaver.travelSpeed > 0f) ||
                                    (projectilePrefab.TryGetComponent(out DaggerController dagger) && dagger.acceleration > 0f) ||
                                    (projectilePrefab.TryGetComponent(out MissileController missile) && (missile.maxVelocity > 0f || missile.acceleration > 0f)) ||
                                    (projectilePrefab.TryGetComponent(out ProjectileCharacterController characterController) && characterController.velocity > 0f) ||
                                    (projectilePrefab.TryGetComponent(out ProjectileOwnerOrbiter ownerOrbiter) && ownerOrbiter.degreesPerSecond > 0f) ||
                                    (projectilePrefab.TryGetComponent(out SoulSearchController soulSearch) && soulSearch.maxVelocity > 0f) ||
                                    (projectilePrefab.TryGetComponent(out Rigidbody rigidbody) && !rigidbody.isKinematic);

                    bool isStationary = !isMoving;
                    _isStationaryLookup[i] = isStationary;

                    // Just because this code thinks the projectile is stationary, doesn't mean it has to be.
                    // It could be modded or just have an edge case that the above code doesn't account for
                    projectilePrefab.AddComponent<ProjectileMovementTracker>();
                }
            }

            BlacklistProjectile("DrifterJunkCubeProjectile");
            BlacklistProjectile("JunkCubePrefab");

            RoR2Application.onLoad += onLoad;
        }

        private static void onLoad()
        {
            HashSet<int> projectileIndexBlacklist = [];

            foreach (GameObject projectilePrefab in _collectingProjectilePrefabsBlacklist)
            {
                int projectileIndex = ProjectileCatalog.GetProjectileIndex(projectilePrefab);
                if (projectileIndex >= 0)
                {
                    projectileIndexBlacklist.Add(projectileIndex);
                }
            }

            foreach (string projectileName in _collectingProjectileNamesBlacklist)
            {
                int projectileIndex = ProjectileCatalog.FindProjectileIndex(projectileName);
                if (projectileIndex >= 0)
                {
                    projectileIndexBlacklist.Add(projectileIndex);
                }
            }

            if (projectileIndexBlacklist.Count > 0)
            {
                _projectileBlacklist = [.. projectileIndexBlacklist];
                Array.Sort(_projectileBlacklist);
            }

            _collectingProjectilePrefabsBlacklist = null;
            _collectingProjectileNamesBlacklist = null;
        }

        /// <summary>
        /// Adds a projectile to the mod's blacklist. Projectiles in the blacklist will not have their positions or rotations changed on spawn. Projectiles auto-detected as stationary will be excluded by default.
        /// </summary>
        /// <param name="projectilePrefab">The projectile to add</param>
        /// <exception cref="InvalidOperationException"><see cref="RoR2Application.onLoad"/> has executed.</exception>
        public static void BlacklistProjectile(GameObject projectilePrefab)
        {
            if (_collectingProjectilePrefabsBlacklist == null)
                throw new InvalidOperationException("Cannot add to blacklist after game has loaded");

            _collectingProjectilePrefabsBlacklist.Add(projectilePrefab);
        }

        /// <summary>
        /// <inheritdoc cref="BlacklistProjectile(GameObject)"/>
        /// </summary>
        /// <param name="projectileName">The name of the projectile to add</param>
        /// <exception cref="InvalidOperationException"><see cref="RoR2Application.onLoad"/> has executed.</exception>
        public static void BlacklistProjectile(string projectileName)
        {
            if (_collectingProjectileNamesBlacklist == null)
                throw new InvalidOperationException("Cannot add to blacklist after game has loaded");

            _collectingProjectileNamesBlacklist.Add(projectileName);
        }

        static void reportProjectileStationary(int projectileIndex, bool stationary)
        {
            if (ArrayUtils.IsInBounds(_isStationaryLookup, projectileIndex))
            {
                if (_isStationaryLookup[projectileIndex] != stationary)
                {
                    _isStationaryLookup[projectileIndex] = stationary;
                    Log.Debug($"Observed projectile '{ProjectileCatalog.projectileNames[projectileIndex]}' stationary: {stationary}");
                }
            }

            GameObject projectilePrefab = ProjectileCatalog.GetProjectilePrefab(projectileIndex);
            if (projectilePrefab && projectilePrefab.TryGetComponent(out ProjectileMovementTracker movementTracker))
            {
                GameObject.Destroy(movementTracker);
                Log.Debug($"Removed movement tracker from projectile '{projectilePrefab.name}'");
            }
        }

        public static bool IsStationaryProjectile(int projectileIndex)
        {
            return ArrayUtils.GetSafe(_isStationaryLookup, projectileIndex);
        }

        public static bool IsProjectileBlacklisted(int projectileIndex)
        {
            return Array.BinarySearch(_projectileBlacklist, projectileIndex) >= 0;
        }

        public static bool ShouldModifyFireDirection(int projectileIndex)
        {
            return !IsProjectileBlacklisted(projectileIndex) && !IsStationaryProjectile(projectileIndex);
        }

        sealed class ProjectileMovementTracker : MonoBehaviour
        {
            const float MovementEpsilon = 0.1f;

            ProjectileController _projectileController;

            Vector3 _startPosition;
            bool _hasMoved;

            void Awake()
            {
                _projectileController = GetComponent<ProjectileController>();
            }

            void Start()
            {
                _startPosition = transform.position;
            }

            void FixedUpdate()
            {
                if (!_hasMoved && (transform.position - _startPosition).sqrMagnitude >= MovementEpsilon * MovementEpsilon)
                {
                    _hasMoved = true;
                }
            }

            void OnDestroy()
            {
                int catalogIndex = _projectileController ? _projectileController.catalogIndex : -1;
                if (catalogIndex >= 0)
                {
                    reportProjectileStationary(catalogIndex, !_hasMoved);
                }
            }
        }
    }
}
