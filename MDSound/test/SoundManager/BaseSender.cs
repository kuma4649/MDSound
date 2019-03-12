namespace SoundManager
{
    public class BaseSender : BaseMakerSender
    {
        protected long Counter = 0;
        protected int Dev = 0;
        protected int Typ = 0;
        protected int Adr = 0;
        protected int Val = 0;
        protected object[] Ex = null;

        protected int ringBufferSize;
    }

}
