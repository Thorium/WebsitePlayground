import * as React from "react";
import * as ReactDOM from "react-dom";
import * as _ from "lodash";

interface NavbarProps { companyId: string; }
class CompanyWebNavBar extends React.Component<NavbarProps, any> {
  constructor(props) {
    super(props);
    this.handleClick = this.handleClick.bind(this);        
    this.getLink = this.getLink.bind(this);       
    this.state = {menuOn: false};
  }
  handleClick () {
    this.setState({menuOn: !this.state.menuOn});
    return false;
  }
  getLink (itm) {
      return itm + ".html";
  }
  render () {
    var menutoggle = [];
    var menubar = [];

    var companyUrl = this.getLink("company") + (this.props.companyId!==""? "#/item/"+this.props.companyId : "");
    if(this.state.menuOn){
      menubar.push(
        <div id="mainMenu" key="menubar" className="icon-bar three-up">
          <a className="item" href={this.getLink("index")}>
            <span className="white fa fa-search"></span>
            <p className="white">Search</p>
          </a>
          <a className="item" href={companyUrl}>
            <span className="white fa fa-database"></span>
            <p className="white">Company page</p>
          </a>
          <a className="item" href="https://github.com/Thorium/WebsitePlayground" target="_blank">
            <span className="white fa fa-github"></span>
            <p className="white">External GitHub link</p>
          </a>
        </div>
        );
    }
    menutoggle.push(<section key="left" className="left-small">
    <a id="hamburger" className="left-off-canvas-toggle menu-icon" href="#" onClick={this.handleClick}><span></span></a>
    </section>);
    return (
      <div>
		<nav className="tab-bar desktop-navbar">
		  {menutoggle}
		  <section className="middle tab-bar-section middletopic">
            <a className="white mainTitle" href={this.getLink("index")}>Company Web</a>
	      </section>
		  <section className="right">
		  </section>
      </nav>
		{menubar}
	  </div>
    );
  }
};

interface CompanyProps { key: any; company: any; buyStocks: any; }
class AvailableCompany extends React.Component<CompanyProps, any> {
    constructor(props) {
      super(props);
      this.handleClick = this.handleClick.bind(this);        
    }
    handleClick () {
        this.props.buyStocks(this.props.company.CompanyName, 50);
    }
    render () {
        var logoImage = [];
        var webPage = [];
        var company = this.props.company;
        const floatRight: React.CSSProperties = {"float":"right"};
        if(company.Image!==null){
            logoImage.push(<a className="th" key="logo" href={company.Image.Fields}><img src={company.Image.Fields} /></a>);
        }
        if(company.Url!==null){
            webPage.push(<span key={company.Url.Fields}>Website:
                           <a href={company.Url.Fields} target="_blank">{company.Url.Fields}</a>
                         </span>);
        }
        return (
              <div key="resultPanel" className="panel searchresultItem">
                  {logoImage}
                  <div>
	                  <div className="darkgreen desktop-company-name"><h3>{company.CompanyName}</h3>
                      <span className="boldstyle" style={floatRight}>{webPage}</span>
                      </div>
                      <input type="button" className="button radius" value="Buy 50 stocks!" onClick={this.handleClick} />
                  </div>
              </div>
        );
  }
};

interface CompaniesProps { companies: Array<any>; buyStocks: any; }
class AvailableCompaniesList extends React.Component<CompaniesProps, any> {
  render () {
      var buyTheseStocks = this.props.buyStocks;
      var companies = {};
      if(this.props.companies !== null && this.props.companies.length !== 0){
          companies = _.map(this.props.companies, function(company, idx:number) {
              return (<AvailableCompany key={idx} company={company} buyStocks={buyTheseStocks} />);
          });
      } else {
          companies = "No companies found.";
      }
      return (<div>{companies}</div>);
  }
};

export function renderAvailableCompanies(theCompanies, buyStocks) {
  let mount = document.getElementById('companies');
  if(mount!==null){
    ReactDOM.unmountComponentAtNode(mount);
    ReactDOM.render(
      <AvailableCompaniesList companies={theCompanies} buyStocks={buyStocks} />,
      mount
    );
  }
}

export function renderNavBar(companyId) {
  var navbard = document.getElementById('navbar');
  if(navbard!==null){
    ReactDOM.unmountComponentAtNode(navbard);
    ReactDOM.render(
      <CompanyWebNavBar companyId={companyId} />,
      navbard
    );
  }
}
