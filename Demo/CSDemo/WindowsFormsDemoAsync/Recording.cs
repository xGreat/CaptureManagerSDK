﻿using CaptureManagerToCSharpProxy;
using CaptureManagerToCSharpProxy.Interfaces;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Windows.Forms;
using System.Xml;
using WPFRecording;

namespace WindowsFormsDemoAsync
{
    public partial class Recording : Form
    {
        CaptureManager mCaptureManager = null;

        ISessionAsync mSession = null;

        ISessionControlAsync mISessionControl = null;

        ISinkControlAsync mSinkControl = null;

        ISourceControlAsync mSourceControl = null;

        IEncoderControlAsync mEncoderControl = null;

        AbstractSink mSink = null;

        public XmlNode mSelectedSourceXmlNode = null;

        class ContainerItem
        {
            public string mFriendlyName = "SourceItem";

            public XmlNode mXmlNode;

            public override string ToString()
            {
                return mFriendlyName;
            }
        }

        public Recording()
        {
            InitializeComponent();
        }

        protected override async void OnLoad(EventArgs e)
        {
            base.OnLoad(e);

            try
            {
                mCaptureManager = new CaptureManager("CaptureManager.dll");
            }
            catch (System.Exception)
            {
                try
                {
                    mCaptureManager = new CaptureManager();
                }
                catch (System.Exception)
                {

                }
            }

            if (mCaptureManager == null)
                return;


            mSourceControl = await mCaptureManager.createSourceControlAsync();

            mISessionControl = await mCaptureManager.createSessionControlAsync();

            mSinkControl = await mCaptureManager.createSinkControlAsync();

            mEncoderControl = await mCaptureManager.createEncoderControlAsync();

            System.Xml.XmlDocument doc = new System.Xml.XmlDocument();

            string lxmldoc = await mCaptureManager.getCollectionOfSourcesAsync();

            if (string.IsNullOrEmpty(lxmldoc))
                return;

            doc.LoadXml(lxmldoc);

            var lSourceNodes = doc.DocumentElement.ChildNodes;// .SelectNodes("//*[Source.Attributes/Attribute[@Name='MF_DEVSOURCE_ATTRIBUTE_MEDIA_TYPE']/Value.ValueParts/ValuePart[@Value='MFMediaType_Video']]");

            if (lSourceNodes != null)
            {
                foreach (var item in lSourceNodes)
                {
                    var lNode = (XmlNode)item;

                    if (lNode != null)
                    {
                        var lvalueNode = lNode.SelectSingleNode("Source.Attributes/Attribute[@Name='MF_DEVSOURCE_ATTRIBUTE_FRIENDLY_NAME']/SingleValue/@Value");

                        ContainerItem lSourceItem = new ContainerItem()
                        {
                            mFriendlyName = lvalueNode.Value,
                            mXmlNode = lNode
                        };

                        sourceComboBox.Items.Add(lSourceItem);
                    }


                }
            }
        }

        private void sourceComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {

            var lSelectedSourceItem = (ContainerItem)sourceComboBox.SelectedItem;

            if (lSelectedSourceItem == null)
                return;

            var lSubTypesNode = lSelectedSourceItem.mXmlNode.SelectNodes("PresentationDescriptor/StreamDescriptor/MediaTypes/MediaType/MediaTypeItem[@Name='MF_MT_SUBTYPE']/SingleValue/@Value");

            if (lSubTypesNode == null)
                return;

            mSelectedSourceXmlNode = lSelectedSourceItem.mXmlNode;

            streamComboBox.Items.Clear();

            foreach (XmlNode item in lSubTypesNode)
            {
                var lSubType = item.Value.Replace("MFVideoFormat_", "");

                lSubType = lSubType.Replace("MFAudioFormat_", "");

                if (!streamComboBox.Items.Contains(lSubType))
                    streamComboBox.Items.Add(lSubType);
            }
        }

        private void streamComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            var lCurrentSubType = (string)streamComboBox.SelectedItem;

            var lCurrentSourceNode = mSelectedSourceXmlNode as XmlNode;

            if (lCurrentSourceNode == null)
                return;

            var lMediaTypeNodes = lCurrentSourceNode.SelectNodes("PresentationDescriptor/StreamDescriptor/MediaTypes/MediaType[MediaTypeItem[@Name='MF_MT_SUBTYPE']/SingleValue[@Value='MFVideoFormat_" + lCurrentSubType + "']]");

            if (lMediaTypeNodes == null)
                return;

            if (lMediaTypeNodes.Count == 0)
            {

                lMediaTypeNodes = lCurrentSourceNode.SelectNodes("PresentationDescriptor/StreamDescriptor/MediaTypes/MediaType[MediaTypeItem[@Name='MF_MT_SUBTYPE']/SingleValue[@Value='MFAudioFormat_" + lCurrentSubType + "']]");

                if (lMediaTypeNodes == null)
                    return;

                if (lMediaTypeNodes.Count == 0)
                    return;

            }

            mediaTypeComboBox.Items.Clear();

            foreach (var item in lMediaTypeNodes)
            {
                var lNode = (XmlNode)item;

                if (lNode != null)
                {

                    var lStreamNode = lCurrentSourceNode.SelectSingleNode("PresentationDescriptor/StreamDescriptor");

                    if (lStreamNode == null)
                        return;

                    var lvalueNode = lStreamNode.SelectSingleNode("@MajorType");

                    string mTitle = "";
                   
                    if(lvalueNode != null && lvalueNode.Value == "MFMediaType_Video")
                    {

                        lvalueNode = lNode.SelectSingleNode("MediaTypeItem[@Name='MF_MT_FRAME_SIZE']/Value.ValueParts/ValuePart[1]/@Value");

                        mTitle = lvalueNode.Value;

                        lvalueNode = lNode.SelectSingleNode("MediaTypeItem[@Name='MF_MT_FRAME_SIZE']/Value.ValueParts/ValuePart[2 ]/@Value");

                        mTitle += "x" + lvalueNode.Value;

                        lvalueNode = lNode.SelectSingleNode("MediaTypeItem[@Name='MF_MT_FRAME_RATE']/RatioValue/@Value");

                        mTitle += ", " + lvalueNode.Value + " FPS, ";

                        lvalueNode = lNode.SelectSingleNode("MediaTypeItem[@Name='MF_MT_SUBTYPE']/SingleValue/@Value");

                        mTitle += lvalueNode.Value.Replace("MFVideoFormat_", "");
                    }
                    else if (lvalueNode != null && lvalueNode.Value == "MFMediaType_Audio")
                        {

                            lvalueNode = lNode.SelectSingleNode("MediaTypeItem[@Name='MF_MT_AUDIO_BITS_PER_SAMPLE']/SingleValue/@Value");

                            if (lvalueNode != null)
                            mTitle = lvalueNode.Value;

                            lvalueNode = lNode.SelectSingleNode("MediaTypeItem[@Name='MF_MT_AUDIO_NUM_CHANNELS']/SingleValue/@Value");

                            if (lvalueNode != null)
                            mTitle += "x" + lvalueNode.Value;

                            lvalueNode = lNode.SelectSingleNode("MediaTypeItem[@Name='MF_MT_AUDIO_SAMPLES_PER_SECOND']/SingleValue/@Value");

                            mTitle += ", ";

                            lvalueNode = lNode.SelectSingleNode("MediaTypeItem[@Name='MF_MT_SUBTYPE']/SingleValue/@Value");

                            if (lvalueNode != null)
                            mTitle += lvalueNode.Value.Replace("MFVideoFormat_", "");
                        }


                    ContainerItem lSourceItem = new ContainerItem()
                    {
                        mFriendlyName = mTitle,// lvalueNode.Value.Replace("MFMediaType_", ""),
                        mXmlNode = lNode
                    };

                    mediaTypeComboBox.Items.Add(lSourceItem);
                }
            }
        }

        private async void mediaTypeComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            var lCurrentSubType = (string)streamComboBox.SelectedItem;

            var lCurrentSourceNode = mSelectedSourceXmlNode as XmlNode;

            if (lCurrentSourceNode == null)
                return;

            var lMediaTypesNode = lCurrentSourceNode.SelectNodes("PresentationDescriptor/StreamDescriptor/MediaTypes/MediaType[MediaTypeItem[@Name='MF_MT_SUBTYPE']/SingleValue[@Value='MFVideoFormat_" + lCurrentSubType + "']]");

            if (lMediaTypesNode == null)
                return;

            var lStreamNode = lCurrentSourceNode.SelectSingleNode("PresentationDescriptor/StreamDescriptor");

            if (lStreamNode == null)
                return;

            var lValueNode = lStreamNode.SelectSingleNode("@MajorTypeGUID");
                       
            string lXPath = "EncoderFactories/Group[@GUID='blank']/EncoderFactory";

            lXPath = lXPath.Replace("blank", lValueNode.Value);


            string lxmldoc = await mCaptureManager.getCollectionOfEncodersAsync();

            System.Xml.XmlDocument doc = new System.Xml.XmlDocument();

            doc.LoadXml(lxmldoc);

            var lEncoderNodes = doc.SelectNodes(lXPath);

            encoderComboBox.Items.Clear();

            foreach (var item in lEncoderNodes)
            {
                
                var lNode = (XmlNode)item;

                if (lNode != null)
                {
                    var lvalueNode = lNode.SelectSingleNode("@Title");
                    
                    ContainerItem lSourceItem = new ContainerItem()
                    {
                        mFriendlyName = lvalueNode.Value,
                        mXmlNode = lNode
                    };

                    encoderComboBox.Items.Add(lSourceItem);
                }
            }

        }

        private async void encoderComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            do
            {

                if (mEncoderControl == null)
                    break;



                var lSelectedEncoderItem = (ContainerItem)encoderComboBox.SelectedItem;

                if (lSelectedEncoderItem == null)
                    return;

                var lselectedNode = lSelectedEncoderItem.mXmlNode;

                if (lselectedNode == null)
                    break;

                var lEncoderNameAttr = lselectedNode.Attributes["Title"];

                if (lEncoderNameAttr == null)
                    break;

                var lCLSIDEncoderAttr = lselectedNode.Attributes["CLSID"];

                if (lCLSIDEncoderAttr == null)
                    break;

                Guid lCLSIDEncoder;

                if (!Guid.TryParse(lCLSIDEncoderAttr.Value, out lCLSIDEncoder))
                    break;


                var lSelectedSourceItem = (ContainerItem)sourceComboBox.SelectedItem;

                if (lSelectedSourceItem == null)
                    return;

                var lSourceNode = lSelectedSourceItem.mXmlNode;

                if (lSourceNode == null)
                    return;

                var lNode = lSourceNode.SelectSingleNode(
            "Source.Attributes/Attribute" +
            "[@Name='MF_DEVSOURCE_ATTRIBUTE_SOURCE_TYPE_VIDCAP_SYMBOLIC_LINK' or @Name='MF_DEVSOURCE_ATTRIBUTE_SOURCE_TYPE_AUDCAP_SYMBOLIC_LINK']" +
            "/SingleValue/@Value");

                if (lNode == null)
                    return;

                string lSymbolicLink = lNode.Value;


                uint lStreamIndex = 0;

                var lSelectedMediaTypeItem = (ContainerItem)mediaTypeComboBox.SelectedItem;

                if (lSelectedMediaTypeItem == null)
                    return;
                
                lSourceNode = lSelectedMediaTypeItem.mXmlNode;

                if (lSourceNode == null)
                    return;

                lNode = lSourceNode.SelectSingleNode("@Index");

                if (lNode == null)
                    return;

                uint lMediaTypeIndex = 0;

                if (!uint.TryParse(lNode.Value, out lMediaTypeIndex))
                {
                    return;
                }
                               

                if (mSourceControl == null)
                    return;

                object lOutputMediaType = await mSourceControl.getSourceOutputMediaTypeAsync(
                    lSymbolicLink,
                    lStreamIndex,
                    lMediaTypeIndex);

                string lMediaTypeCollection = await mEncoderControl.getMediaTypeCollectionOfEncoderAsync(
                    lOutputMediaType,
                    lCLSIDEncoder);
                
                XmlDocument lEncoderModedoc = new XmlDocument();

                lEncoderModedoc.LoadXml(lMediaTypeCollection);

                var lEncoderNodes = lEncoderModedoc.SelectNodes("EncoderMediaTypes/Group");

                encoderModeComboBox.Items.Clear();

                foreach (var item in lEncoderNodes)
                {

                    lNode = (XmlNode)item;

                    if (lNode != null)
                    {
                        var lvalueNode = lNode.SelectSingleNode("@Title");

                        ContainerItem lSourceItem = new ContainerItem()
                        {
                            mFriendlyName = lvalueNode.Value,
                            mXmlNode = lNode
                        };

                        encoderModeComboBox.Items.Add(lSourceItem);
                    }
                }

                
            } while (false);
        }

        private void label5_Click(object sender, EventArgs e)
        {

        }

        private void encoderModeComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            var lCurrentSourceNode = mSelectedSourceXmlNode as XmlNode;

            if (lCurrentSourceNode == null)
                return;

            var lStreamNode = lCurrentSourceNode.SelectSingleNode("PresentationDescriptor/StreamDescriptor");

            if (lStreamNode == null)
                return;
            
            var lSelectedEncoderModeItem = (ContainerItem)encoderModeComboBox.SelectedItem;

            if (lSelectedEncoderModeItem == null)
                return;

            var lcompressedMediaTypeNodes = lSelectedEncoderModeItem.mXmlNode.SelectNodes("MediaTypes/MediaType");

            compressedMediaTypeComboBox.Items.Clear();

            foreach (var item in lcompressedMediaTypeNodes)
            {

                var lNode = (XmlNode)item;

                if (lNode != null)
                {

                    var lvalueNode = lStreamNode.SelectSingleNode("@MajorType");

                    string mTitle = "";

                    if (lvalueNode != null && lvalueNode.Value == "MFMediaType_Video")
                    {

                        lvalueNode = lNode.SelectSingleNode("MediaTypeItem[@Name='MF_MT_FRAME_SIZE']/Value.ValueParts/ValuePart[1]/@Value");

                        mTitle = lvalueNode.Value;

                        lvalueNode = lNode.SelectSingleNode("MediaTypeItem[@Name='MF_MT_FRAME_SIZE']/Value.ValueParts/ValuePart[2 ]/@Value");

                        mTitle += "x" + lvalueNode.Value;

                        lvalueNode = lNode.SelectSingleNode("MediaTypeItem[@Name='MF_MT_FRAME_RATE']/RatioValue/@Value");

                        mTitle += ", " + lvalueNode.Value + " FPS, ";

                        lvalueNode = lNode.SelectSingleNode("MediaTypeItem[@Name='MF_MT_SUBTYPE']/SingleValue/@Value");

                        mTitle += lvalueNode.Value.Replace("MFVideoFormat_", "");
                    }
                    else if (lvalueNode != null && lvalueNode.Value == "MFMediaType_Audio")
                    {

                        lvalueNode = lNode.SelectSingleNode("MediaTypeItem[@Name='MF_MT_AUDIO_BITS_PER_SAMPLE']/SingleValue/@Value");

                        if (lvalueNode != null)
                            mTitle = lvalueNode.Value;

                        lvalueNode = lNode.SelectSingleNode("MediaTypeItem[@Name='MF_MT_AUDIO_NUM_CHANNELS']/SingleValue/@Value");

                        if (lvalueNode != null)
                            mTitle += "x" + lvalueNode.Value;

                        lvalueNode = lNode.SelectSingleNode("MediaTypeItem[@Name='MF_MT_AUDIO_SAMPLES_PER_SECOND']/SingleValue/@Value");

                        mTitle += ", ";

                        lvalueNode = lNode.SelectSingleNode("MediaTypeItem[@Name='MF_MT_SUBTYPE']/SingleValue/@Value");

                        if (lvalueNode != null)
                            mTitle += lvalueNode.Value.Replace("MFVideoFormat_", "");
                    }


                    ContainerItem lSourceItem = new ContainerItem()
                    {
                        mFriendlyName = mTitle,// lvalueNode.Value.Replace("MFMediaType_", ""),
                        mXmlNode = lNode
                    };


                    compressedMediaTypeComboBox.Items.Add(lSourceItem);
                }
            }
        }

        private async void compressedMediaTypeComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            string lxmldoc = await mCaptureManager.getCollectionOfSinksAsync();
            
            var doc = new System.Xml.XmlDocument();

            doc.LoadXml(lxmldoc);

            var lsinkFactoryNodes = doc.SelectNodes("SinkFactories/SinkFactory");

            sinkComboBox.Items.Clear();

            foreach (var item in lsinkFactoryNodes)
            {

                var lNode = (XmlNode)item;

                if (lNode != null)
                {
                    var lAttr = lNode.Attributes["GUID"];

                    if (lAttr == null)
                        throw new System.Exception("GUID is empty");

                    if (lAttr.Value == "{D6E342E3-7DDD-4858-AB91-4253643864C2}")
                    {
                        var lvalueNode = lNode.SelectSingleNode("@Title");

                        ContainerItem lSourceItem = new ContainerItem()
                        {
                            mFriendlyName = lvalueNode.Value,
                            mXmlNode = lNode
                        };

                        sinkComboBox.Items.Add(lSourceItem);
                    }

                }
            }
        }

        private void sinkComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {

            var lSelectedSinkItem = (ContainerItem)sinkComboBox.SelectedItem;

            if (lSelectedSinkItem == null)
                return;

            var lContainerNodes = lSelectedSinkItem.mXmlNode.SelectNodes("Value.ValueParts/ValuePart");

            formatComboBox.Items.Clear();

            foreach (var item in lContainerNodes)
            {

                var lNode = (XmlNode)item;

                if (lNode != null)
                {
                    var lvalueNode = lNode.SelectSingleNode("@Value");

                    ContainerItem lSourceItem = new ContainerItem()
                    {
                        mFriendlyName = lvalueNode.Value,
                        mXmlNode = lNode
                    };

                    formatComboBox.Items.Add(lSourceItem);

                }
            }
        }

        private async void button1_Click(object sender, EventArgs e)
        {

            do
            {
                var lSelectedFormatItem = (ContainerItem)formatComboBox.SelectedItem;

                if (lSelectedFormatItem == null)
                    return;


                var lselectedNode = lSelectedFormatItem.mXmlNode;

                if (lselectedNode == null)
                    break;

                var lSelectedAttr = lselectedNode.Attributes["Value"];

                if (lSelectedAttr == null)
                    break;

                String limageSourceDir = System.IO.Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);

                SaveFileDialog lsaveFileDialog = new SaveFileDialog();

                lsaveFileDialog.InitialDirectory = limageSourceDir;

                lsaveFileDialog.DefaultExt = "." + lSelectedAttr.Value.ToLower();

                lsaveFileDialog.AddExtension = true;

                lsaveFileDialog.CheckFileExists = false;

                lsaveFileDialog.Filter = "Media file (*." + lSelectedAttr.Value.ToLower() + ")|*." + lSelectedAttr.Value.ToLower();

                var lresult = lsaveFileDialog.ShowDialog();

                if (lresult != DialogResult.OK)
                    break;

                mDo.Enabled = true;

                lSelectedAttr = lselectedNode.Attributes["GUID"];

                if (lSelectedAttr == null)
                    break;

                var lFileSinkFactory = await mSinkControl.createFileSinkFactoryAsync(
                    Guid.Parse(lSelectedAttr.Value));

                mSink = new FileSink(lFileSinkFactory);

                mSink.setOptions(lsaveFileDialog.FileName);

            }
            while (false);
        }

        private async void mDo_Click(object sender, EventArgs e)
        {
            mDo.Enabled = false;

            do
            {

                if (mSession != null)
                {
                    await mSession.closeSessionAsync();

                    mSession = null;

                    mDo.Text = "Stopped";

                    break;
                }

                if (mSink == null)
                    break;

                var lSelectedSourceItem = (ContainerItem)sourceComboBox.SelectedItem;

                if (lSelectedSourceItem == null)
                    break;

                var lSourceNode = lSelectedSourceItem.mXmlNode;

                if (lSourceNode == null)
                    break;

                var lNode = lSourceNode.SelectSingleNode(
                    "Source.Attributes/Attribute" +
                    "[@Name='MF_DEVSOURCE_ATTRIBUTE_SOURCE_TYPE_VIDCAP_SYMBOLIC_LINK' or @Name='MF_DEVSOURCE_ATTRIBUTE_SOURCE_TYPE_AUDCAP_SYMBOLIC_LINK']" +
                    "/SingleValue/@Value");

                if (lNode == null)
                    break;

                string lSymbolicLink = lNode.Value;

                uint lStreamIndex = 0;

                var lSelectedMediaTypeItem = (ContainerItem)mediaTypeComboBox.SelectedItem;

                if (lSelectedMediaTypeItem == null)
                    break;

                lSourceNode = lSelectedMediaTypeItem.mXmlNode;

                if (lSourceNode == null)
                    break;

                lNode = lSourceNode.SelectSingleNode("@Index");

                if (lNode == null)
                    break;

                uint lMediaTypeIndex = 0;

                if (!uint.TryParse(lNode.Value, out lMediaTypeIndex))
                    break;

                object lOutputMediaType = await mSourceControl.getSourceOutputMediaTypeAsync(
                            lSymbolicLink,
                            lStreamIndex,
                            lMediaTypeIndex);


                var lSelectedEncoderItem = (ContainerItem)encoderComboBox.SelectedItem;

                if (lSelectedEncoderItem == null)
                    break;


                var lselectedNode = lSelectedEncoderItem.mXmlNode;

                if (lselectedNode == null)
                    break;

                var lEncoderNameAttr = lselectedNode.Attributes["Title"];

                if (lEncoderNameAttr == null)
                    break;

                var lCLSIDEncoderAttr = lselectedNode.Attributes["CLSID"];

                if (lCLSIDEncoderAttr == null)
                    break;

                Guid lCLSIDEncoder;

                if (!Guid.TryParse(lCLSIDEncoderAttr.Value, out lCLSIDEncoder))
                    break;

                var lEncoderNodeFactory = await mEncoderControl.createEncoderNodeFactoryAsync(
                    lCLSIDEncoder);




                var lSelectedEncoderModeItem = (ContainerItem)encoderModeComboBox.SelectedItem;

                if (lSelectedEncoderModeItem == null)
                    break;

                lselectedNode = lSelectedEncoderModeItem.mXmlNode;

                if (lselectedNode == null)
                    break;

                var lGUIDEncodingModeAttr = lselectedNode.Attributes["GUID"];

                if (lGUIDEncodingModeAttr == null)
                    break;

                Guid lGUIDEncodingMode;

                if (!Guid.TryParse(lGUIDEncodingModeAttr.Value, out lGUIDEncodingMode))
                    break;


                if (compressedMediaTypeComboBox.SelectedIndex < 0)
                    break;

                object lCompressedMediaType = await lEncoderNodeFactory.createCompressedMediaTypeAsync(
                    lOutputMediaType,
                    lGUIDEncodingMode,
                    70,
                    (uint)compressedMediaTypeComboBox.SelectedIndex);

                var lOutputNode = await mSink.getOutputNodeAsync(lCompressedMediaType);

                var lIEncoderNodeFactory = await mEncoderControl.createEncoderNodeFactoryAsync(
                    lCLSIDEncoder);

                object lEncoderNode = await lIEncoderNodeFactory.createEncoderNodeAsync(
                    lOutputMediaType,
                    lGUIDEncodingMode,
                    70,
                    (uint)compressedMediaTypeComboBox.SelectedIndex,
                    lOutputNode);

                object lSourceMediaNode = await mSourceControl.createSourceNodeWithDownStreamConnectionAsync(
                            lSymbolicLink,
                            lStreamIndex,
                            lMediaTypeIndex,
                            lEncoderNode);

                List<object> lSourcesList = new List<object>();

                lSourcesList.Add(lSourceMediaNode);

                mSession = await mISessionControl.createSessionAsync(lSourcesList.ToArray());

                if (mSession == null)
                    break;

                if (mSession != null)
                    await mSession.startSessionAsync(0, Guid.Empty);

                mDo.Text = "Record is executed!!!";

            } while (false);

            mDo.Enabled = true;
        }
    }
}
