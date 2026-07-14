using UnityEngine;
using CarFactoryIdle.Core;

namespace CarFactoryIdle.UI
{
    /// <summary>Bridge between GameRoot (whose GameFacade instance only exists once its own Awake has
    /// run) and every UI screen script. Screens read GameServices.Facade instead of each holding
    /// their own reference.
    ///
    /// DefaultExecutionOrder(100) makes this Awake run after GameRoot's (order 0) regardless of
    /// GameObject/component order, and Unity guarantees every Awake in the scene finishes before any
    /// Start begins — so by the time any UI script's Start() runs, Facade is always already set.</summary>
    [DefaultExecutionOrder(100)]
    [RequireComponent(typeof(GameRoot))]
    public class GameServices : MonoBehaviour
    {
        public static GameFacade Facade { get; private set; }
        public static GameRoot Root { get; private set; }

        private void Awake()
        {
            Root = GetComponent<GameRoot>();
            Facade = Root.Facade;
        }
    }
}
