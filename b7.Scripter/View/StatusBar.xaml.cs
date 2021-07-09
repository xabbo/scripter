using System;
using System.Windows.Controls;

namespace b7.Scripter.View
{
    public partial class StatusBar : UserControl
    {
        public StatusBar()
        {
            InitializeComponent();
        }

        public void Initialize()
        {
            // this.scriptManager = scriptManager;

            //var componentManager = scriptManager.ComponentManager;

            //var profileManager = componentManager.GetComponent<ProfileManager>();
            //var roomManager = componentManager.GetComponent<RoomManager>();
            //var entityManager = componentManager.GetComponent<EntityManager>();
            //var furniManager = componentManager.GetComponent<FurniManager>();

            //UserDataAvailable = scriptManager.Interceptor.Dispatcher.IsAttached(profileManager, ProfileManager.Features.UserData);
            //HasUserData = profileManager.UserData != null;
            //if (!HasUserData) profileManager.LoadedUserData += (s, e) => HasUserData = true;

            //RoomManagerAvailable = scriptManager.Interceptor.Dispatcher.IsAttached(roomManager, MessageGroups.Default);
            //BeenInRoom = false;
            //IsInRoom = false;
            //UserCount = -1;
            //BotCount = -1;
            //PetCount = -1;
            //FurniCount = -1;

            //roomManager.Entered += (s, e) =>
            //{
            //    IsInRoom = true;
            //    BeenInRoom = true;
            //};
            //roomManager.Left += (s, e) =>
            //{
            //    IsInRoom = false;
            //    UserCount = -1;
            //    BotCount = -1;
            //    PetCount = -1;
            //    FurniCount = -1;
            //};

            //void updateEntityCounts()
            //{
            //    UserCount = entityManager.Users.Count();
            //    PetCount = entityManager.Pets.Count();
            //    BotCount = entityManager.Bots.Count();
            //}

            //entityManager.EntitiesAdded += (s, e) => updateEntityCounts();
            //entityManager.EntityRemoved += (s, e) => updateEntityCounts();

            //void updateFurniCount() => FurniCount = furniManager.Furni.Count();

            //furniManager.FloorItemsLoaded += (s, e) => updateFurniCount();
            //furniManager.WallItemsLoaded += (s, e) => updateFurniCount();
            //furniManager.FloorItemAdded += (s, e) => updateFurniCount();
            //furniManager.WallItemAdded += (s, e) => updateFurniCount();
            //furniManager.FloorItemRemoved += (s, e) => updateFurniCount();
            //furniManager.WallItemRemoved += (s, e) => updateFurniCount();
        }

        private void UpdateFurniCount()
        {

        }
    }
}
