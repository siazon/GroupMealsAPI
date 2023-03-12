namespace Takeaway.Service.Contract.Entities.Settings
{

    public class WsSetting:WsEntity
    {
        public string Name { get; set; }
        public string SettingValue { get; set; }
        public bool? IsServer { get; set; }
         
    }
}