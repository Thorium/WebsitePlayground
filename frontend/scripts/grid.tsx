import tools = require("./tools");
import signalhub = require("./signalhub");
import * as React from "react";
import * as ReactDOM from "react-dom";
import * as _ from "lodash";

interface IItemProps { data: any; key: number; key2: number; orderid: number; inPageId: any; }

let allowSelectingCompanyItems = false;

class MyItem extends React.Component<IItemProps, any> {
    constructor(props) {
        super(props);
        this.getIdx = this.getIdx.bind(this);
        // this.callServer = this.callServer.bind(this);
        // this.refreshStatus = this.refreshStatus.bind(this);
        this.gotoPage = this.gotoPage.bind(this);
    }
    getIdx () {
        return this.props.data.id;
    }

    // refreshStatus(inPageId) {
    //     $(".buttonloader").css("display", "block");
    //     let pagesize = $("#pageSize").val() === undefined || $("#pageSize").val() === null ? 20 : $("#pageSize").val();
    //     signalhub.hubConnector.invoke("GetCompanyList").then(res2 => {
    //         let totalcount = res2.Item1;
    //         let data2 = res2.Item2;
    //         tools.generatePagination(totalcount, inPageId, pagesize);
    //         renderList(data2, inPageId);
    //         $(".buttoncontainer").show();
    //         $(".buttonloader").css("display", "none");
    //         $("#ResultInfo").html("");
    //     }).catch(function(err) {
    //         console.log('Response: ' + err);
    //         $(".buttoncontainer").show();
    //         $("#ResultInfo").html("");
    //         $(".buttonloader").css("display", "none");
    //     });
    // }

    // callServer () {
    //     let inPageId = this.props.inPageId;
    //     $("#tinyLoader").show();
    //     $(".buttoncontainer").hide();
    //     someServerHub.invoke("ExecuteActionToCompany").then(d => {
    //         $("#tinyLoader").hide();
    //         if(d==="Failed"){
    //             alert("Action failed!");
    //             $("#ResultInfo").html("");
    //             $(".buttoncontainer").show();
    //         } else {
    //             alert("Done!");
    //             this.refreshStatus(inPageId);
    //         }
    //     }).catch(function(err) {
    //         $("#tinyLoader").hide();
    //         alert("Failed."); 
    //         console.log('Response: ' + err);
    //         $("#ResultInfo").html("");
    //         $(".buttoncontainer").show();
    //     });
    // }

    gotoPage () {
        document.location.href = "company.html#/item/" + this.getIdx();
        return false;
    }

    componentDidMount (){
        const cid = this.props.data.id;
        $(document).on("tap click", '#check_'+cid, function( event, data ){
            setTimeout(function() { $('#check_'+cid).prop('checked', !$('#check_'+cid).prop('checked')); }, 10); 
            event.stopPropagation();
            event.preventDefault();
            event.stopImmediatePropagation();
            return false;
        });
        
        $('#select-all').off();
        $('#select-all').click(function(event) {   
            if((this as HTMLInputElement).checked) {
                $('input:checkbox.companyMultiselect').each(function() { (this as HTMLInputElement).checked = true; });
            } else {
                $('input:checkbox.companyMultiselect').each(function() { (this as HTMLInputElement).checked = false; });
            }
        });

    }
    render () {
        const companyData = this.props.data;
        const id = companyData.id;
        const key2 = "ext1"+id;
        let buttons = [];
        let info = [];
        
        const isLocked = // to disable selection 
            (companyData.locked !== undefined &&
             companyData.locked !== null &&
             companyData.locked !== ""
            );

        let newCompanyInfo = [];

        if(this.props.data.founded !== undefined && this.props.data.founded !== null && 
                new Date(this.props.data.founded).getFullYear() >= (new Date()).getFullYear() ){

            newCompanyInfo.push(
                <div className="row" key={"rowitn"+id}>
                    <div className="medium-12 columns">
                        This is a new company.
                    </div>
                </div>
            );
        }

        const formId = "companyform"+id;
        const companyIdx = "company"+id;
        const contentclass = "content";
        const companyIdxLink = "#" + companyIdx;
        const tabIdx = "tabcompany"+id;
        const ke2 = "liacc1" + this.props.key2;
        let reservationInfo = isLocked ?
            (<div key={"reserv" + id} className={isLocked ? "yellowBg row" : "row"}>
                <div className="medium-4 columns">
                    <label className="middle"><span className="fa fa-lock fa-fw"></span> Locked</label>
                </div>
                <div className="medium-8 columns">
                    {this.props.data.locked}
                </div>
            </div>)
            : "";

        const hasLock = 
            isLocked ? 
                (<span key={"lock" + id} className="fa fa-lock fa-fw" title="Locked"></span>) : 
                allowSelectingCompanyItems?
                (<input type="checkbox" key={"check_" + id} id={"check_" + id} className="companyMultiselect" 
                    name={"check_" + id} value={id} />) : "";

        buttons.push(<div key={key2+"b"} className="buttoncontainer" style={{display: "inline"}}>
            <input type="button" className = "right button" id="selectBtn"
                    value="Edit company" onClick={this.gotoPage} style={{marginLeft: '4px', marginRight: '4px'}}
                    title="This will go to the edit page." />
            </div>);

        // First <a> is a header row of a grid. By clicking that, it will open more details.
        // The header row should match the columns defined in MyItemsList below.
        return (
          <li key={ke2} className="accordion-navigation">
          <a id={tabIdx} href={companyIdxLink}> 
            <div className="row">
                <div className="small-6 medium-6 columns centered">
                    {hasLock} {companyData.name}
                </div>
                <div className="medium-6 columns centered hide-for-small-only">
                    {companyData.ceo}
                </div>
            </div>
          </a>
          <div id={companyIdx} className={contentclass}>
          <form method="post" id={formId} data-abide="ajax">
          <input type="hidden" id="companyId" value={id} />
            <fieldset>
                <legend>Company Info</legend>


                {reservationInfo}
                <div className="row">
                    <div className="medium-3 columns">
                        <label className="middle"><span className="fa fa-bank fa-fw"></span> Name</label>
                    </div>
                    <div className="medium-9 columns">
                            {companyData.name}
                    </div>
                </div>
                <div style={{height: "0.3rem"}} />
                <div className="row">
                    <div className="medium-3 columns">
                        <label><span className="fa fa-calendar-o fa-fw"></span> Founded</label>
                    </div>
                    <div className="medium-3 columns" title={companyData.founded.replace("T", " ")}>
                        {(new Date(companyData.founded)).toLocaleDateString()}
                    </div>
                    <div className="medium-3 columns">
                        <label><span className="fa fa-user fa-fw"></span> CEO</label>
                    </div>
                    <div className="medium-3 columns">
                        {companyData.ceo}
                    </div>
                </div>
                {info}
                {newCompanyInfo}

                <div className="row">
                    <div className="medium-12 columns">
                        <small>...more details to follow...</small>
                    </div>
                </div>

                <div className="row">
                    <div className="medium-12 columns">
                        <span style={{display:"none"}} className="buttonloader"><span className="fa fa-spinner fa-spin fa-1x"></span></span>
                        {buttons}
                   </div>
                </div>
            </fieldset>
          </form>
          </div>
          </li>
        );
    }
};

interface IItemsListProps { key: string; dataItems : Array<any>; pageId:any; }
class MyItemsList extends React.Component<IItemsListProps, any> {
    render () {

        let itemlist = this.props.dataItems;

        let pageId = this.props.pageId;

        var renderItems = {};
        if(this.props !== null && this.props.dataItems !== null && this.props.dataItems.length!==0){
            renderItems = _.map(itemlist, function(itemdata:any, idx:number) {
                return (<MyItem key={idx} key2={idx} data={itemdata} orderid={(idx+1)} inPageId={pageId} />);
            });
        } else {
            renderItems = (<li>No items found!</li>);
        }

        return (
        <div>
            <div className="accordeontable">
                <div className="row">
                    <div className="small-6 medium-6 columns centered">
                        <input type="checkbox" title="Select all" id="select-all" name="select-all" 
                            style={{marginRight: "5px", display: allowSelectingCompanyItems ? "visible" : "none" }} />
                        Company name
                    </div>
                    <div className="medium-6 columns centered hide-for-small-only">
                        CEO
                    </div>
                </div>
            </div>
            <ul className="accordion" data-accordion>
                {renderItems}
            </ul>
        </div>
        );
    }
};

export function renderList(data, pageId) {

    let mount = document.getElementById('companyList');
    if(mount!==null){
        ReactDOM.unmountComponentAtNode(mount);
        ReactDOM.render(
            <MyItemsList dataItems={data} key="itemlist1" pageId={pageId} />,
            mount
        );
    }
}

export function collectMultiselected(data, pageId) {
    let selectedItems = [];
    $('input:checkbox.companyMultiselect').each(function () {
        let t = this as HTMLInputElement;
        if(t.checked){
            selectedItems.push($(this).val());
        }
    });
    return selectedItems;
}
