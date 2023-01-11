using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sandbox;


namespace Sandbox
{
    public partial class Grenade : ModelEntity, IUse
    {
        [Net, Predicted] public TimeSince TimeSinceActivated { get; set; }

        private bool activated;
        private int activatedTime;
        private List<Line> lineList = new List<Line>();
        private bool doTakeDamage = true;

        public string ExplosionEffect { get; set; }

        public override void Spawn()
        {
            SetModel("grenade.vmdl");

            TimeSinceActivated = 0;

            Log.Info("Hi! I'm addd grenade.");

            base.Spawn();
            SetupPhysicsFromModel(
                PhysicsMotionType.Dynamic
            );

        }

        public override void TakeDamage(DamageInfo info)
        {
            if (info.Damage > 1 && doTakeDamage)
            {
                Log.Info("foo");
                detonate();
                doTakeDamage = false;
            }
        }

        public bool IsUsable(Entity user)
        {
            return true;
        }

        private void generateFragmentation()
        {
            Vector3 randomFragVector;
            Ray tempRay;
            Vector3 fragDirectionVector;

            for (int i = 0; i < 200; i++)
            {
                randomFragVector = Vector3.Random;
                tempRay = new Ray(Position, randomFragVector);
                fragDirectionVector = tempRay.Project(600);

                lineList.Add(new Line(Position, fragDirectionVector));
            }
        }

        public bool OnUse(Entity user)
        {
            Log.Info("Hi! I'm an activated grenade.");

            detonate();

            return false;
        }

        public void detonate()
        {
            //50 feet / 15 m
            //50 - 100 fragmentations

            if (!activated)
            {
                Sound.FromWorld("pinpulled", Position);
            }

            activated = true;

            var origin = Position;
            var radius = 300;
            var damage = 110;
            var force = new Vector3(0, 0, 0);

            if (TimeSinceActivated >= 5)
            {
                Log.Info("Boom!");
                generateFragmentation();

                Sound.FromWorld("small-explosion", Position);


                Particles.Create("particles/grenade_he_explosion", Position);
                var objects = Entity.FindInSphere(Position, 300);

                foreach (var obj in objects)
                {
                    // Entity check
                    if (obj is not ModelEntity ent || !ent.IsValid())
                        continue;

                    if (ent.LifeState != LifeState.Alive)
                        continue;

                    if (!ent.PhysicsBody.IsValid())
                        continue;

                    //if (ent.IsWorld)
                    //   continue;

                    // Dist check
                    var targetPos = ent.PhysicsBody.MassCenter;
                    var dist = Vector3.DistanceBetween(origin, targetPos);
                    if (dist > radius)
                        continue;

                    // Temp solution
                    var tr = Trace.Ray(origin, targetPos)
                        .Ignore(this)
                        .WorldOnly()
                        .Run();

                    if (tr.Fraction < 1.0f) continue;

                    var distanceMul = 1.0f - Math.Clamp(dist / radius, 0.0f, 1.0f);
                    var realDamage = damage * distanceMul;
                    var realForce = force * distanceMul;
                    var forceDir = (targetPos - origin).Normal;

                    if (ent.Name != this.Name)
                    {
                        ent.TakeDamage(DamageInfo.FromExplosion(origin, forceDir * realForce, realDamage));
                    }
                }

                Delete();
            }

        }

        [Event.Tick]
        void Tick()
        {
            if (!activated)
            {
                TimeSinceActivated = 0;
            }
            else
            {
                //Log.Info($"Countdown detonates at 5 seconds: {TimeSinceActivated}");
                detonate();

            }

        }
    }
}
