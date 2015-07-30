/// <reference path="../../paket-files/aFarkas/html5shiv/dist/html5shiv.min.js" /> 
/// <reference path="../../paket-files/ajax.aspnetcdn.com/jquery.min.js" /> 
/// <reference path="../../paket-files/cdnjs.cloudflare.com/knockout-min.js" /> 
/// <reference path="../../paket-files/code.jquery.com/jquery-ui.min.js" /> 
/// <reference path="../../paket-files/reactjs/react-bower/react.js" /> 
/// <reference path="../../paket-files/SignalR/bower-signalr/jquery.signalR.js" /> 
/// <reference path="../../paket-files/underscorejs.org/underscore-min.js" /> 
/// <reference path="../../paket-files/zurb/bower-foundation/js/foundation.min.js" /> 

var CompanyWebNavBar = React.createClass({
  getInitialState: function() {
    return {menuOn: false};
  },
  handleClick: function(event) {
    this.setState({menuOn: !this.state.menuOn});
  },
  render: function() {
    profileInfo = [];
    menutoggle = [];	
    menubar = [];

    var companyUrl = "company.html" + (this.props.companyId!==""? "#/item/"+this.props.companyId : "");
    if(this.state.menuOn){
      menubar.push(
        <div id="mainMenu" className="icon-bar three-up">
          <a className="item" href="index.html">
            <span className="white fa fa-search"></span>
            <label>Search</label>
          </a>
          <a className="item" href={companyUrl}>
            <span className="white fa fa-database"></span>
            <label>Company page</label>
          </a>
          <a className="item" href="https://github.com/Thorium/WebsitePlayground" target="_blank">
            <span className="white fa fa-github"></span>
            <label>External GitHub link</label>
          </a>
        </div>
        );
    }
    menutoggle.push(<section className="left-small"><a id="hamburger" className="left-off-canvas-toggle menu-icon" href="#" onClick={this.handleClick}><span></span></a></section>);
    return (
      <div>
		<nav className="tab-bar desktop-navbar">
		  {menutoggle}
		  <section className="middle tab-bar-section middletopic">
            <a className="white mainTitle" href="index.html">Company Web</a>
	      </section>
		  <section className="right">
		  </section>
      </nav>
		{menubar}
	  </div>
    );
  }
});

var AvailableCompany = React.createClass({
    handleClick: function(event) {
        signalHub.server.buyStocks(this.props.company.CompanyName, 50);
    },
    render: function() {
        var logoImage = [];
        var webPage = [];
        var company = this.props.company;
        var floatRight = {"float":"right"};
        if(company.Image!==null){
            logoImage.push(<a className="th" href={company.Image.Fields}><img src={company.Image.Fields} /></a>);
        }
        if(company.Url!==null){
            webPage.push(<span>Website: <a href={company.Url.Fields} target="_blank">{company.Url.Fields}</a></span>);
        }
        return (
              <div className="panel searchresultItem">
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
});

var AvailableCompaniesList = React.createClass({
  render: function() {
      var companies = {};
      if(this.props.companies !== null){
          companies = _.map(this.props.companies, function(company) { return (<AvailableCompany company={company} />); });
      } else {
          companies = "No companies found.";
      }
      return (<div>{companies}</div>);
  }
});

function renderAvailableCompanies(theCompanies) {
  React.render(
    <AvailableCompaniesList companies={theCompanies}/>,
    document.getElementById('companies')
  );
}

function renderNavBar(companyId) {
  React.render(
    <CompanyWebNavBar companyId={companyId} />,
    document.getElementById('navbar')
  );
}
